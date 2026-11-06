using System;
using System.Collections;
using UnityEngine;
using ValorChronicle.Core.Bootstrap;
using ValorChronicle.Core.Logging;
using ValorChronicle.Core.Random;
using ValorChronicle.Data.Definitions;

namespace ValorChronicle.Battle.Board.Presentation
{
    public sealed class BattleBoardController : MonoBehaviour,
        IBattleBoardMutationSink,
        IBattleBoardElementConversionSink,
        IBattleBoardShuffleElementConversionSink,
        IBattleBoardStateSource
    {
        [SerializeField]
        private BattleBoardView boardView = null;

        [SerializeField]
        [Min(0f)]
        private float initialDropDuration = 0.45f;

        [SerializeField]
        [Min(0f)]
        private float initialDropColumnStagger = 0.04f;

        [SerializeField]
        [Min(0f)]
        private float swapDuration = 0.15f;

        [SerializeField]
        [Min(0f)]
        private float removalDuration = 0.10f;

        [SerializeField]
        [Min(0f)]
        private float collapseDuration = 0.16f;

        [SerializeField]
        [Min(0f)]
        private float refillDuration = 0.18f;

        [SerializeField]
        [Min(0f)]
        private float shuffleDuration = 0.16f;

        private bool initialized;
        private bool observesCascadeStepPresentation;
        private bool isActionInProgress;
        private bool initialBoardReadyPublished;
        private bool activeActionStarted;
        private int initializationVersion;
        private int actionVersion;
        private long nextActionId;
        private long activeActionId;
        private IRandomSource randomSource;
        private BoardBlockIdGenerator blockIdGenerator;
        private BoardGenerator boardGenerator;
        private BoardRefiller boardRefiller;
        private BoardShuffler boardShuffler;
        private BoardRockMutationResolver rockMutationResolver;
        private BoardRootMutationResolver rootMutationResolver;
        private BoardElementConversionResolver elementConversionResolver;
        private BoardShuffleElementConversionResolver
            shuffleElementConversionResolver;
        private BoardSwapActionResolver swapActionResolver;
        private BoardActionPresentationTimings actionTimings;
        private IEnumerator activePresentationSequence;
        private BoardSwapActionResult activeActionResult;
        private int nextCascadeStepPresentationIndex;
        private BattleBoardMutationCommand activeMutationCommand;
        private IBattleBoardMutationResult activeMutationResult;
        private BoardState activeMutationBeforeBoard;
        private Action<BattleBoardMutationCompletion> activeMutationCompletion;
        private int stableRootCount;
        private bool hasStableBoardSnapshot;

        public event Action InitialBoardReady;
        public event Action<BoardActionExecution> BoardActionStarted;
        public event Action<BoardCascadeStepPresentation>
            CascadeStepPresented;
        public event Action<int> MatchGroupsRemovalStarted;
        public event Action<BoardActionCompletion> BoardActionFinished;
        public event Action StableBoardChanged;
        public event Action ExternalInputDisabled;

        public BoardState CurrentBoard { get; private set; }
        public BoardSwapActionResult LastSwapActionResult { get; private set; }
        public BoardRockMutationResult LastRockMutationResult
        {
            get;
            private set;
        }
        public BoardRootMutationResult LastRootMutationResult
        {
            get;
            private set;
        }
        public BoardElementConversionResult LastElementConversionResult
        {
            get;
            private set;
        }
        public BoardShuffleElementConversionResult
            LastShuffleElementConversionResult { get; private set; }
        public bool IsInitialized => initialized;
        public bool IsBoardReady { get; private set; }
        public bool HasInitialBoardReady => initialBoardReadyPublished;
        private bool isExternalInputEnabled = true;

        public bool IsExternalInputEnabled
        {
            get => isExternalInputEnabled;
            set
            {
                if (isExternalInputEnabled == value)
                {
                    return;
                }

                isExternalInputEnabled = value;
                if (!value)
                {
                    ExternalInputDisabled?.Invoke();
                }
            }
        }
        public bool CanAcceptBoardInput => IsExternalInputEnabled
            && initialized
            && IsBoardReady
            && !isActionInProgress
            && HasResolverGraph
            && boardView != null
            && !boardView.IsAnimating;
        public bool CanAcceptElementConversion => CanAcceptBoardInput;
        public bool CanAcceptShuffleElementConversion => CanAcceptBoardInput;

        public int GetRootCount()
        {
            return hasStableBoardSnapshot ? stableRootCount : 0;
        }

        private bool HasResolverGraph => randomSource != null
            && blockIdGenerator != null
            && boardGenerator != null
            && boardRefiller != null
            && boardShuffler != null
            && rockMutationResolver != null
            && rootMutationResolver != null
            && elementConversionResolver != null
            && shuffleElementConversionResolver != null
            && swapActionResolver != null
            && actionTimings != null;

        private void Start()
        {
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Initial board setup failed.",
                    this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            initializationVersion++;
            if (!isActionInProgress)
            {
                return;
            }

            bool wasMutationPresentation = activeMutationCompletion != null;
            long interruptedActionId = activeActionId;
            actionVersion++;
            IEnumerator interruptedSequence = activePresentationSequence;
            activePresentationSequence = null;
            Exception interruptionFailure = null;
            try
            {
                if (interruptedSequence is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception exception)
            {
                interruptionFailure = exception;
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Failed to stop the board presentation.",
                    this);
            }

            isActionInProgress = false;
            UpdateReadyStateAfterPresentation(
                "The interrupted board presentation did not stabilize.");
            if (wasMutationPresentation)
            {
                CompleteActiveMutation(
                    BattleBoardMutationCompletionStatus.Failed,
                    interruptionFailure ?? new InvalidOperationException(
                        "Board mutation presentation was interrupted."));
            }
            else
            {
                TryCompleteBoardAction(
                    interruptedActionId,
                    BoardActionCompletionStatus.Interrupted,
                    interruptionFailure);
            }
        }

        private void OnDestroy()
        {
            StopObservingCascadeStepPresentation();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ValidateInitializationSettings();
            GameBootstrapper bootstrapper = GameBootstrapper.Instance;
            if (bootstrapper == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper is not available.");
            }

            IRandomSource sharedRandomSource = bootstrapper.RandomSource;
            if (sharedRandomSource == null)
            {
                throw new InvalidOperationException(
                    "GameBootstrapper.RandomSource is not initialized.");
            }

            boardView.ConfigureBlockAnimationTimeSources(
                () => bootstrapper.GamePauseService?.IsPaused
                    ?? Mathf.Approximately(Time.timeScale, 0f),
                () => Time.unscaledDeltaTime);

            var sharedIdGenerator = new BoardBlockIdGenerator();
            var moveAnalyzer = new BoardMoveAnalyzer();
            var generator = new BoardGenerator(
                sharedRandomSource,
                BoardMatchFinder.FindMatches,
                moveAnalyzer,
                sharedIdGenerator);
            var refiller = new BoardRefiller(
                sharedRandomSource,
                sharedIdGenerator);
            var cascadeStepResolver = new BoardCascadeStepResolver(refiller);
            var cascadeResolver = new BoardCascadeResolver(
                cascadeStepResolver);
            var shuffler = new BoardShuffler(
                sharedRandomSource,
                moveAnalyzer);
            var actionResolver = new BoardSwapActionResolver(
                moveAnalyzer,
                cascadeResolver,
                shuffler);
            var rockResolver = new BoardRockMutationResolver(
                sharedRandomSource,
                sharedIdGenerator,
                moveAnalyzer,
                shuffler);
            var rootResolver = new BoardRootMutationResolver(
                sharedRandomSource,
                sharedIdGenerator,
                moveAnalyzer,
                shuffler);
            var conversionResolver = new BoardElementConversionResolver(
                sharedRandomSource,
                moveAnalyzer);
            var shuffleConversionResolver =
                new BoardShuffleElementConversionResolver(
                    sharedRandomSource,
                    moveAnalyzer);
            BoardActionPresentationTimings timings = CreateActionTimings();

            BoardState generatedBoard = generator.Generate();
            CurrentBoard = generatedBoard;
            IsBoardReady = false;
            CaptureStableBoardSnapshot(notifyObservers: false);

            IEnumerator initialDrop;
            try
            {
                initialDrop = boardView.RenderInitialDrop(
                    generatedBoard,
                    initialDropDuration,
                    initialDropColumnStagger);
            }
            catch
            {
                CurrentBoard = null;
                throw;
            }

            randomSource = sharedRandomSource;
            blockIdGenerator = sharedIdGenerator;
            boardGenerator = generator;
            boardRefiller = refiller;
            boardShuffler = shuffler;
            rockMutationResolver = rockResolver;
            rootMutationResolver = rootResolver;
            elementConversionResolver = conversionResolver;
            shuffleElementConversionResolver = shuffleConversionResolver;
            swapActionResolver = actionResolver;
            actionTimings = timings;
            StartObservingCascadeStepPresentation();
            initialized = true;
            int version = ++initializationVersion;
            StartCoroutine(RenderInitialBoard(initialDrop, version));
        }

        public bool TryExecuteSwap(BoardSwap swap)
        {
            if (!CanAcceptBoardInput)
            {
                return false;
            }

            BoardState beforeBoard = CurrentBoard;
            BoardSwapActionResult result;
            try
            {
                result = swapActionResolver.Resolve(beforeBoard, swap);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board swap resolution failed.",
                    this);
                return false;
            }

            LastSwapActionResult = result;
            if (result.Status == BoardSwapActionStatus.NotSwappable)
            {
                UpdateReadyStateAfterPresentation(
                    "The not-swappable board no longer matches its View.");
                return true;
            }

            CurrentBoard = result.Board;
            IsBoardReady = false;
            isActionInProgress = true;
            int version = ++actionVersion;
            long actionId;

            try
            {
                actionId = StartBoardAction(result);
                activePresentationSequence =
                    boardView.PlaySwapActionSequence(
                        beforeBoard,
                        result,
                        actionTimings);
                StartCoroutine(RunBoardAction(version, actionId));
                return true;
            }
            catch (Exception exception)
            {
                activePresentationSequence = null;
                isActionInProgress = false;
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board presentation failed to start.",
                    this);
                UpdateReadyStateAfterPresentation(
                    "The failed board presentation did not stabilize.");
                TryCompleteBoardAction(
                    activeActionId,
                    BoardActionCompletionStatus.Failed,
                    exception);
                return false;
            }
        }

        public bool TryExecuteSwap(
            BoardPosition first,
            BoardPosition second)
        {
            return TryExecuteSwap(new BoardSwap(first, second));
        }

        public bool TryConvertElements(
            ElementType targetElement,
            int requestedCount,
            out BoardElementConversionResult result)
        {
            if (!Enum.IsDefined(typeof(ElementType), targetElement))
            {
                throw new ArgumentOutOfRangeException(nameof(targetElement));
            }

            if (requestedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            result = null;
            if (!CanAcceptBoardInput || elementConversionResolver == null)
            {
                return false;
            }

            BoardState beforeBoard = CurrentBoard;
            isActionInProgress = true;
            try
            {
                BoardElementConversionResult resolved =
                    elementConversionResolver.Resolve(
                        beforeBoard,
                        targetElement,
                        requestedCount);
                LastElementConversionResult = resolved;
                result = resolved;
                if (!resolved.Succeeded)
                {
                    return false;
                }

                if (ReferenceEquals(beforeBoard, resolved.Board))
                {
                    throw new InvalidOperationException(
                        "Element conversion must return an independent "
                            + "BoardState.");
                }

                CurrentBoard = resolved.Board;
                IsBoardReady = false;
                try
                {
                    boardView.Render(CurrentBoard);
                    if (!boardView.MatchesBoard(CurrentBoard))
                    {
                        throw new InvalidOperationException(
                            "The converted board View does not match "
                                + "CurrentBoard.");
                    }
                }
                catch (Exception exception)
                {
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleBoard] Element conversion presentation failed.",
                        this);
                    RestoreAfterFailedElementConversion(beforeBoard);
                    result = null;
                    return false;
                }

                IsBoardReady = true;
                CaptureStableBoardSnapshot(notifyObservers: true);
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Element conversion resolution failed.",
                    this);
                result = null;
                return false;
            }
            finally
            {
                isActionInProgress = false;
            }
        }

        public bool TryShuffleAndConvertElements(
            ElementType targetElement,
            int requestedCount,
            out BoardShuffleElementConversionResult result)
        {
            if (!Enum.IsDefined(typeof(ElementType), targetElement))
            {
                throw new ArgumentOutOfRangeException(nameof(targetElement));
            }

            if (requestedCount < 0 || requestedCount > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedCount));
            }

            result = null;
            if (!CanAcceptBoardInput
                || shuffleElementConversionResolver == null)
            {
                return false;
            }

            BoardState beforeBoard = CurrentBoard;
            isActionInProgress = true;
            try
            {
                BoardShuffleElementConversionResult resolved =
                    shuffleElementConversionResolver.Resolve(
                        beforeBoard,
                        targetElement,
                        requestedCount);
                LastShuffleElementConversionResult = resolved;
                result = resolved;
                if (!resolved.Succeeded)
                {
                    return false;
                }

                if (ReferenceEquals(beforeBoard, resolved.Board))
                {
                    throw new InvalidOperationException(
                        "Shuffle conversion must return an independent "
                            + "BoardState.");
                }

                CurrentBoard = resolved.Board;
                IsBoardReady = false;
                try
                {
                    // The composite Resolver is pure. Commit and render only
                    // its final Board, never its intermediate shuffle.
                    boardView.Render(CurrentBoard);
                    if (!boardView.MatchesBoard(CurrentBoard))
                    {
                        throw new InvalidOperationException(
                            "The shuffle-converted board View does not "
                                + "match CurrentBoard.");
                    }
                }
                catch (Exception exception)
                {
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleBoard] Shuffle conversion presentation "
                            + "failed.",
                        this);
                    RestoreAfterFailedElementConversion(beforeBoard);
                    result = null;
                    return false;
                }

                IsBoardReady = true;
                CaptureStableBoardSnapshot(notifyObservers: true);
                return true;
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Shuffle conversion resolution failed.",
                    this);
                result = null;
                return false;
            }
            finally
            {
                isActionInProgress = false;
            }
        }

        public bool TryExecuteMutation(
            BattleBoardMutationCommand command,
            Action<BattleBoardMutationCompletion> completion)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (completion == null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (!initialized
                || !IsBoardReady
                || isActionInProgress
                || boardView == null
                || boardView.IsAnimating
                || rockMutationResolver == null
                || rootMutationResolver == null)
            {
                return false;
            }

            IBattleBoardMutationResult result;
            try
            {
                switch (command.Kind)
                {
                    case BattleBoardMutationKind.CreateRock:
                        LastRockMutationResult = rockMutationResolver.Resolve(
                            CurrentBoard,
                            command.RequestedCount,
                            command.MaximumCount);
                        result = LastRockMutationResult;
                        break;
                    case BattleBoardMutationKind.CreateRoot:
                        LastRootMutationResult = rootMutationResolver.Resolve(
                            CurrentBoard,
                            command.RequestedCount,
                            command.MaximumCount);
                        result = LastRootMutationResult;
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Unsupported Board mutation: {command.Kind}.");
                }
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board mutation resolution failed.",
                    this);
                completion(new BattleBoardMutationCompletion(
                    command,
                    result: null,
                    status: BattleBoardMutationCompletionStatus.Failed,
                    failure: exception));
                return true;
            }

            if (!result.Succeeded)
            {
                GameLogger.Error(
                    $"[BattleBoard] {command.Kind} mutation failed.",
                    this);
                completion(new BattleBoardMutationCompletion(
                    command,
                    result,
                    BattleBoardMutationCompletionStatus.Failed));
                return true;
            }

            BoardState beforeBoard = CurrentBoard;
            if (ReferenceEquals(beforeBoard, result.Board))
            {
                throw new InvalidOperationException(
                    "Board mutation must return an independent BoardState.");
            }

            CurrentBoard = result.Board;
            IsBoardReady = false;
            isActionInProgress = true;
            if (result.WasShuffled)
            {
                try
                {
                    BoardState preShuffleBoard = BuildPreShuffleBoard(
                        beforeBoard,
                        result);
                    boardView.Render(preShuffleBoard);
                    activeMutationCommand = command;
                    activeMutationResult = result;
                    activeMutationBeforeBoard = beforeBoard;
                    activeMutationCompletion = completion;
                    activePresentationSequence =
                        boardView.PlayShuffleTransition(
                            preShuffleBoard,
                            result.ShuffleResult,
                            shuffleDuration);
                    int version = ++actionVersion;
                    StartCoroutine(RunMutationPresentation(version));
                    return true;
                }
                catch (Exception exception)
                {
                    activePresentationSequence = null;
                    activeMutationCommand = null;
                    activeMutationResult = null;
                    activeMutationBeforeBoard = null;
                    activeMutationCompletion = null;
                    isActionInProgress = false;
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleBoard] Board mutation shuffle presentation "
                            + "failed to start.",
                        this);
                    RestoreAfterFailedMutation(beforeBoard);
                    completion(new BattleBoardMutationCompletion(
                        command,
                        result,
                        BattleBoardMutationCompletionStatus.Failed,
                        exception));
                    return true;
                }
            }

            Exception presentationFailure = null;
            try
            {
                boardView.Render(CurrentBoard);
            }
            catch (Exception exception)
            {
                presentationFailure = exception;
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Board mutation presentation failed.",
                    this);
            }
            finally
            {
                isActionInProgress = false;
                UpdateReadyStateAfterPresentation(
                    "The Board mutation View does not match CurrentBoard.",
                    notifyStableBoardChanged: false);
            }

            if (presentationFailure != null || !IsBoardReady)
            {
                RestoreAfterFailedMutation(beforeBoard);
                completion(new BattleBoardMutationCompletion(
                    command,
                    result,
                    BattleBoardMutationCompletionStatus.Failed,
                    presentationFailure
                        ?? new InvalidOperationException(
                            "Board mutation presentation did not stabilize.")));
                return true;
            }

            completion(new BattleBoardMutationCompletion(
                command,
                result,
                BattleBoardMutationCompletionStatus.Completed));
            StableBoardChanged?.Invoke();
            return true;
        }

        private IEnumerator RunMutationPresentation(int version)
        {
            Exception failure = null;
            while (actionVersion == version
                && activePresentationSequence != null)
            {
                bool hasNext;
                object current = null;
                try
                {
                    hasNext = activePresentationSequence.MoveNext();
                    if (hasNext)
                    {
                        current = activePresentationSequence.Current;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return current;
            }

            if (actionVersion != version)
            {
                yield break;
            }

            activePresentationSequence = null;
            isActionInProgress = false;
            if (failure != null)
            {
                GameLogger.Exception(failure, this);
                GameLogger.Error(
                    "[BattleBoard] Board mutation shuffle presentation failed.",
                    this);
            }

            UpdateReadyStateAfterPresentation(
                "The completed Board mutation shuffle View does not match "
                    + "CurrentBoard.",
                notifyStableBoardChanged: false);
            if (failure == null && !IsBoardReady)
            {
                failure = new InvalidOperationException(
                    "Board mutation shuffle presentation did not stabilize.");
            }

            CompleteActiveMutation(
                failure == null
                    ? BattleBoardMutationCompletionStatus.Completed
                    : BattleBoardMutationCompletionStatus.Failed,
                failure);
        }

        private void CompleteActiveMutation(
            BattleBoardMutationCompletionStatus status,
            Exception failure)
        {
            BattleBoardMutationCommand command = activeMutationCommand;
            IBattleBoardMutationResult result = activeMutationResult;
            BoardState beforeBoard = activeMutationBeforeBoard;
            Action<BattleBoardMutationCompletion> completion =
                activeMutationCompletion;
            activeMutationCommand = null;
            activeMutationResult = null;
            activeMutationBeforeBoard = null;
            activeMutationCompletion = null;
            if (command == null || result == null || completion == null)
            {
                return;
            }

            if (status != BattleBoardMutationCompletionStatus.Completed
                && beforeBoard != null)
            {
                RestoreAfterFailedMutation(beforeBoard);
            }

            completion(new BattleBoardMutationCompletion(
                command,
                result,
                status,
                failure));
            if (status == BattleBoardMutationCompletionStatus.Completed
                && IsBoardReady)
            {
                StableBoardChanged?.Invoke();
            }
        }

        private static BoardState BuildPreShuffleBoard(
            BoardState beforeBoard,
            IBattleBoardMutationResult result)
        {
            if (result.ShuffleResult == null
                || !result.ShuffleResult.WasShuffled)
            {
                throw new InvalidOperationException(
                    "A shuffled Board mutation requires a shuffle result.");
            }

            BoardState preShuffleBoard = beforeBoard.Clone();
            if (result is BoardRockMutationResult rockResult)
            {
                for (int index = 0;
                    index < rockResult.Placements.Count;
                    index++)
                {
                    BoardRockPlacement placement =
                        rockResult.Placements[index];
                    ValidatePlacementSource(
                        preShuffleBoard,
                        placement.Position,
                        placement.ReplacedBlock,
                        "Rock");
                    preShuffleBoard.Set(
                        placement.Position,
                        placement.RockBlock);
                }
            }
            else if (result is BoardRootMutationResult rootResult)
            {
                for (int index = 0;
                    index < rootResult.Placements.Count;
                    index++)
                {
                    BoardRootPlacement placement =
                        rootResult.Placements[index];
                    ValidatePlacementSource(
                        preShuffleBoard,
                        placement.Position,
                        placement.ReplacedBlock,
                        "Root");
                    preShuffleBoard.Set(
                        placement.Position,
                        placement.RootBlock);
                }
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported mutation result: {result.GetType().Name}.");
            }

            return preShuffleBoard;
        }

        private static void ValidatePlacementSource(
            BoardState board,
            BoardPosition position,
            BoardBlock expected,
            string displayName)
        {
            if (!ReferenceEquals(board.Get(position), expected))
            {
                throw new InvalidOperationException(
                    $"A {displayName} placement does not match the source "
                        + $"Board at {position}.");
            }
        }

        private IEnumerator RenderInitialBoard(
            IEnumerator initialDrop,
            int version)
        {
            yield return initialDrop;
            if (initializationVersion != version)
            {
                yield break;
            }

            UpdateReadyStateAfterInitialDrop();
        }

        private void UpdateReadyStateAfterInitialDrop()
        {
            string failureReason = null;
            bool matches = CurrentBoard != null
                && boardView != null
                && !boardView.IsAnimating;
            if (matches)
            {
                matches = boardView.TryValidateCurrentViewLayout(
                    CurrentBoard,
                    "InitialDropComplete",
                    out failureReason);
            }
            else if (CurrentBoard == null)
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=CurrentBoard; Actual=<null>.";
            }
            else if (boardView == null)
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=BattleBoardView; Actual=<null>.";
            }
            else
            {
                failureReason = "Context=InitialDropComplete; " +
                    "Field=IsAnimating; Expected=False; Actual=True.";
            }

            IsBoardReady = matches;
            if (!matches)
            {
                GameLogger.Error(
                    "[BattleBoard] Initial board View mismatch: " +
                    failureReason,
                    this);
            }
            else if (!initialBoardReadyPublished)
            {
                initialBoardReadyPublished = true;
                CaptureStableBoardSnapshot(notifyObservers: true);
                InitialBoardReady?.Invoke();
            }
        }

        private IEnumerator RunBoardAction(int version, long actionId)
        {
            Exception failure = null;
            while (actionVersion == version
                && activePresentationSequence != null)
            {
                bool hasNext;
                object current = null;
                try
                {
                    hasNext = activePresentationSequence.MoveNext();
                    if (hasNext)
                    {
                        current = activePresentationSequence.Current;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return current;
            }

            if (actionVersion != version)
            {
                yield break;
            }

            activePresentationSequence = null;
            isActionInProgress = false;
            if (failure != null)
            {
                GameLogger.Exception(failure, this);
                GameLogger.Error(
                    "[BattleBoard] Board presentation failed.",
                    this);
            }

            UpdateReadyStateAfterPresentation(
                "The completed board View does not match CurrentBoard.");
            TryCompleteBoardAction(
                actionId,
                failure == null
                    ? BoardActionCompletionStatus.Completed
                    : BoardActionCompletionStatus.Failed,
                failure);
        }

        private long StartBoardAction(BoardSwapActionResult result)
        {
            long actionId = checked(++nextActionId);
            activeActionId = actionId;
            activeActionResult = result;
            nextCascadeStepPresentationIndex = 0;
            activeActionStarted = true;
            BoardActionStarted?.Invoke(new BoardActionExecution(
                actionId,
                result));
            return actionId;
        }

        private bool TryCompleteBoardAction(
            long actionId,
            BoardActionCompletionStatus status,
            Exception failure)
        {
            if (!activeActionStarted
                || activeActionResult == null
                || activeActionId != actionId)
            {
                return false;
            }

            BoardSwapActionResult result = activeActionResult;
            activeActionStarted = false;
            activeActionId = 0;
            activeActionResult = null;
            nextCascadeStepPresentationIndex = 0;
            BoardActionFinished?.Invoke(new BoardActionCompletion(
                actionId,
                result,
                status,
                failure));
            return true;
        }

        private void StartObservingCascadeStepPresentation()
        {
            if (observesCascadeStepPresentation)
            {
                return;
            }

            boardView.CascadeStepPresented += HandleCascadeStepPresented;
            boardView.MatchGroupsRemovalStarted +=
                HandleMatchGroupsRemovalStarted;
            observesCascadeStepPresentation = true;
        }

        private void StopObservingCascadeStepPresentation()
        {
            if (!observesCascadeStepPresentation || boardView == null)
            {
                return;
            }

            boardView.CascadeStepPresented -= HandleCascadeStepPresented;
            boardView.MatchGroupsRemovalStarted -=
                HandleMatchGroupsRemovalStarted;
            observesCascadeStepPresentation = false;
        }

        private void HandleMatchGroupsRemovalStarted(
            int cascadeStepIndex,
            BoardCascadeStep step)
        {
            if (!IsCurrentCascadeStep(cascadeStepIndex, step))
            {
                return;
            }

            int matchGroupCount = step.Matches.Count;
            if (matchGroupCount <= 0)
            {
                return;
            }

            PublishMatchGroupsRemovalStarted(matchGroupCount);
        }

        private void HandleCascadeStepPresented(
            int cascadeStepIndex,
            BoardCascadeStep step)
        {
            if (!IsCurrentCascadeStep(cascadeStepIndex, step))
            {
                return;
            }

            nextCascadeStepPresentationIndex++;
            PublishCascadeStepPresented(
                new BoardCascadeStepPresentation(
                    activeActionId,
                    cascadeStepIndex,
                    step));
        }

        private void PublishCascadeStepPresented(
            BoardCascadeStepPresentation presentation)
        {
            Action<BoardCascadeStepPresentation> observers =
                CascadeStepPresented;
            if (observers == null)
            {
                return;
            }

            foreach (Action<BoardCascadeStepPresentation> observer
                in observers.GetInvocationList())
            {
                try
                {
                    observer(presentation);
                }
                catch (Exception exception)
                {
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleBoard] Cascade presentation observer failed.",
                        this);
                }
            }
        }

        private bool IsCurrentCascadeStep(
            int cascadeStepIndex,
            BoardCascadeStep step)
        {
            return activeActionStarted
                && activeActionId > 0
                && activeActionResult?.Status
                    == BoardSwapActionStatus.Resolved
                && activeActionResult.Cascade != null
                && cascadeStepIndex == nextCascadeStepPresentationIndex
                && cascadeStepIndex >= 0
                && cascadeStepIndex < activeActionResult.Cascade.Steps.Count
                && ReferenceEquals(
                    activeActionResult.Cascade.Steps[cascadeStepIndex],
                    step);
        }

        private void PublishMatchGroupsRemovalStarted(int matchGroupCount)
        {
            Action<int> observers = MatchGroupsRemovalStarted;
            if (observers == null)
            {
                return;
            }

            foreach (Action<int> observer in observers.GetInvocationList())
            {
                try
                {
                    observer(matchGroupCount);
                }
                catch (Exception exception)
                {
                    GameLogger.Exception(exception, this);
                    GameLogger.Error(
                        "[BattleBoard] Match removal observer failed.",
                        this);
                }
            }
        }

        private void UpdateReadyStateAfterPresentation(
            string failureMessage,
            bool notifyStableBoardChanged = true)
        {
            bool matches = CurrentBoard != null
                && boardView != null
                && !boardView.IsAnimating
                && boardView.MatchesBoard(CurrentBoard);
            IsBoardReady = matches;
            if (!matches)
            {
                GameLogger.Error($"[BattleBoard] {failureMessage}", this);
            }
            else
            {
                CaptureStableBoardSnapshot(notifyStableBoardChanged);
            }
        }

        private void CaptureStableBoardSnapshot(bool notifyObservers)
        {
            if (CurrentBoard == null)
            {
                return;
            }

            int rootCount = 0;
            for (int y = 0; y < BoardConstants.Height; y++)
            {
                for (int x = 0; x < BoardConstants.Width; x++)
                {
                    BoardBlock block = CurrentBoard.Get(
                        new BoardPosition(x, y));
                    if (block != null
                        && block.BlockType == BoardBlockType.Root)
                    {
                        rootCount++;
                    }
                }
            }

            stableRootCount = rootCount;
            hasStableBoardSnapshot = true;
            if (notifyObservers)
            {
                StableBoardChanged?.Invoke();
            }
        }

        private void RestoreAfterFailedElementConversion(
            BoardState beforeBoard)
        {
            CurrentBoard = beforeBoard;
            IsBoardReady = false;
            try
            {
                boardView.Render(beforeBoard);
                UpdateReadyStateAfterPresentation(
                    "The restored board View does not match CurrentBoard.");
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Failed to restore the board View after "
                        + "an Element conversion presentation failure.",
                    this);
                IsBoardReady = false;
            }
        }

        private void RestoreAfterFailedMutation(BoardState beforeBoard)
        {
            if (beforeBoard == null)
            {
                return;
            }

            CurrentBoard = beforeBoard;
            IsBoardReady = false;
            try
            {
                boardView.Render(beforeBoard);
                UpdateReadyStateAfterPresentation(
                    "The restored Board mutation View does not match "
                        + "CurrentBoard.",
                    notifyStableBoardChanged: false);
            }
            catch (Exception exception)
            {
                GameLogger.Exception(exception, this);
                GameLogger.Error(
                    "[BattleBoard] Failed to restore the Board View after "
                        + "a mutation presentation failure.",
                    this);
                IsBoardReady = false;
            }
        }

        private void ValidateInitializationSettings()
        {
            if (boardView == null)
            {
                throw new InvalidOperationException(
                    "A BattleBoardView must be assigned.");
            }

            ValidateNonNegative(initialDropDuration, "Initial drop duration");
            ValidateNonNegative(
                initialDropColumnStagger,
                "Initial drop column stagger");
            ValidateNonNegative(swapDuration, "Swap duration");
            ValidateNonNegative(removalDuration, "Removal duration");
            ValidateNonNegative(collapseDuration, "Collapse duration");
            ValidateNonNegative(refillDuration, "Refill duration");
            ValidateNonNegative(shuffleDuration, "Shuffle duration");
        }

        private BoardActionPresentationTimings CreateActionTimings()
        {
            return new BoardActionPresentationTimings(
                swapDuration,
                removalDuration,
                collapseDuration,
                refillDuration,
                shuffleDuration);
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (value < 0f)
            {
                throw new InvalidOperationException(
                    $"{name} cannot be negative.");
            }
        }
    }
}
