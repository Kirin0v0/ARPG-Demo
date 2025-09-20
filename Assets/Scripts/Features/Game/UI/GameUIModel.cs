using System;
using Features.Game.Data;
using Framework.Core.LiveData;
using Map;
using VContainer;
using VContainer.Unity;

namespace Features.Game.UI
{
    public class GameUIModel : IGameUIModel, IStartable, IDisposable
    {
        public readonly MutableLiveData<GameUIData> LoadingUI = new(new GameUIData());
        public readonly MutableLiveData<GameLoadingUIData> LoadingData = new(new GameLoadingUIData());
        public readonly MutableLiveData<GameUIData> CutsceneUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> MenuUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> MapUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> CharacterUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> PackageUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> QuestUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> ArchiveUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> DeathUI = new(new GameUIData());

        public readonly MutableLiveData<GameUIData> SystemCommandUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> CommonCommandUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> ComboCommandUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> BattleCommandUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> SkillCommandUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> PlayerResourceUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> MiniMapUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> DialogueUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> TradeUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> NotificationUI = new(new GameUIData());
        public readonly MutableLiveData<GameUIData> TipUI = new(new GameUIData());

        public readonly MutableLiveData<bool> ToHidePlaceholderUI = new(false, LiveDataMode.Debounce);
        private readonly MutableLiveData<bool> _systemUIShowing = new(false, LiveDataMode.Debounce);
        public readonly MutableLiveData<bool> BattleCommandExpanding = new(false, LiveDataMode.Debounce);
        public readonly MutableLiveData<bool> DialogueShowing = new(false, LiveDataMode.Debounce);
        private readonly MutableLiveData<bool> _allowGUIShowing = new(false, LiveDataMode.Debounce);
        private readonly MutableLiveData<bool> _allowCharacterInformationShowing = new(false, LiveDataMode.Debounce);
        private readonly MutableLiveData<bool> _allowOutlineShowing = new(false, LiveDataMode.Debounce);

        [Inject] private MapManager _mapManager;

        public void Start()
        {
            LoadingUI.ObserveForever(CheckSystemUIDataChanged);
            CutsceneUI.ObserveForever(CheckSystemUIDataChanged);
            MenuUI.ObserveForever(CheckSystemUIDataChanged);
            MapUI.ObserveForever(CheckSystemUIDataChanged);
            CharacterUI.ObserveForever(CheckSystemUIDataChanged);
            PackageUI.ObserveForever(CheckSystemUIDataChanged);
            QuestUI.ObserveForever(CheckSystemUIDataChanged);
            ArchiveUI.ObserveForever(CheckSystemUIDataChanged);
            DeathUI.ObserveForever(CheckSystemUIDataChanged);

            _systemUIShowing.ObserveForever(CheckWhetherAllowGUIShow);
            DialogueShowing.ObserveForever(CheckWhetherAllowGUIShow);
            DialogueShowing.ObserveForever(CheckWhetherAllowCharacterInformationShow);
            DialogueShowing.ObserveForever(CheckWhetherAllowOutlineShow);

            // 直接初始化值
            CheckSystemUIDataChanged(default);
            CheckWhetherAllowGUIShow(default);
            CheckWhetherAllowCharacterInformationShow(default);
            CheckWhetherAllowOutlineShow(default);
        }

        public void Dispose()
        {
            LoadingUI.RemoveObserver(CheckSystemUIDataChanged);
            CutsceneUI.RemoveObserver(CheckSystemUIDataChanged);
            MenuUI.RemoveObserver(CheckSystemUIDataChanged);
            MapUI.RemoveObserver(CheckSystemUIDataChanged);
            CharacterUI.RemoveObserver(CheckSystemUIDataChanged);
            PackageUI.RemoveObserver(CheckSystemUIDataChanged);
            QuestUI.RemoveObserver(CheckSystemUIDataChanged);
            ArchiveUI.RemoveObserver(CheckSystemUIDataChanged);
            DeathUI.RemoveObserver(CheckSystemUIDataChanged);

            _systemUIShowing.RemoveObserver(CheckWhetherAllowGUIShow);
            DialogueShowing.RemoveObserver(CheckWhetherAllowGUIShow);
            DialogueShowing.RemoveObserver(CheckWhetherAllowCharacterInformationShow);
            DialogueShowing.RemoveObserver(CheckWhetherAllowOutlineShow);
        }

        public LiveData<GameUIData> GetLoadingUI() => LoadingUI;

        public LiveData<GameLoadingUIData> GetLoadingData() => LoadingData;

        public LiveData<GameUIData> GetCutsceneUI() => CutsceneUI;

        public LiveData<GameUIData> GetMenuUI() => MenuUI;

        public LiveData<GameUIData> GetMapUI() => MapUI;

        public LiveData<GameUIData> GetCharacterUI() => CharacterUI;

        public LiveData<GameUIData> GetPackageUI() => PackageUI;

        public LiveData<GameUIData> GetQuestUI() => QuestUI;

        public LiveData<GameUIData> GetArchiveUI() => ArchiveUI;

        public LiveData<GameUIData> GetDeathUI() => DeathUI;

        public LiveData<GameUIData> GetSystemCommandUI() => SystemCommandUI;
        public LiveData<GameUIData> GetCommonCommandUI() => CommonCommandUI;

        public LiveData<GameUIData> GetComboCommandUI() => ComboCommandUI;

        public LiveData<GameUIData> GetBattleCommandUI() => BattleCommandUI;

        public LiveData<GameUIData> GetSkillCommandUI() => SkillCommandUI;

        public LiveData<GameUIData> GetPlayerResourceUI() => PlayerResourceUI;

        public LiveData<GameUIData> GetMiniMapUI() => MiniMapUI;

        public LiveData<GameUIData> GetDialogueUI() => DialogueUI;

        public LiveData<GameUIData> GetTradeUI() => TradeUI;

        public LiveData<GameUIData> GetNotificationUI() => NotificationUI;

        public LiveData<GameUIData> GetTipUI() => TipUI;

        public LiveData<bool> HidePlaceholderUI() => ToHidePlaceholderUI;

        public LiveData<bool> IsBattleCommandExpanding() => BattleCommandExpanding;

        public LiveData<bool> IsDialogueShowing() => DialogueShowing;

        public LiveData<bool> AllowGUIShowing() => _allowGUIShowing;

        public LiveData<bool> AllowCharacterInformationShowing() => _allowCharacterInformationShowing;

        public LiveData<bool> AllowOutlineShowing() => _allowOutlineShowing;

        private void CheckSystemUIDataChanged(GameUIData data)
        {
            _systemUIShowing.SetValue(
                LoadingUI.Value.Visible ||
                CutsceneUI.Value.Visible ||
                MenuUI.Value.Visible ||
                MapUI.Value.Visible ||
                CharacterUI.Value.Visible ||
                PackageUI.Value.Visible ||
                QuestUI.Value.Visible ||
                ArchiveUI.Value.Visible ||
                DeathUI.Value.Visible
            );
        }

        private void CheckWhetherAllowGUIShow(bool isShowing)
        {
            _allowGUIShowing.SetValue(
                !_systemUIShowing.Value && !DialogueShowing.Value
            );
        }

        private void CheckWhetherAllowCharacterInformationShow(bool value)
        {
            _allowCharacterInformationShowing.SetValue(!DialogueShowing.Value);
        }

        private void CheckWhetherAllowOutlineShow(bool value)
        {
            _allowOutlineShowing.SetValue(!DialogueShowing.Value);
        }

        // private void OnLoadingShow(GameUIData data)
        // {
        //     if (!data.Visible) return;
        //     CutsceneUI.SetValue(CutsceneUI.Value.Close());
        //     MenuUI.SetValue(MenuUI.Value.Close());
        //     PackageUI.SetValue(PackageUI.Value.Close());
        //     QuestUI.SetValue(QuestUI.Value.Close());
        //     SystemCommandUI.SetValue(SystemCommandUI.Value.Close());
        //     PlayerResourceUI.SetValue(PlayerResourceUI.Value.Close());
        //     CommonCommandUI.SetValue(CommonCommandUI.Value.Close());
        //     ComboCommandUI.SetValue(ComboCommandUI.Value.Close());
        //     BattleCommandUI.SetValue(BattleCommandUI.Value.Close());
        //     SkillCommandUI.SetValue(SkillCommandUI.Value.Close());
        //     DialogueUI.SetValue(DialogueUI.Value.Close());
        //     TradeUI.SetValue(TradeUI.Value.Close());
        //     NotificationUI.SetValue(NotificationUI.Value.Close());
        //     TipUI.SetValue(TipUI.Value.Close());
        // }
        //
        // private void OnCutsceneShow(GameUIData data)
        // {
        //     if (!data.Visible) return;
        //     MenuUI.SetValue(MenuUI.Value.Close());
        //     PackageUI.SetValue(PackageUI.Value.Close());
        //     QuestUI.SetValue(QuestUI.Value.Close());
        //     SystemCommandUI.SetValue(SystemCommandUI.Value.Close());
        //     PlayerResourceUI.SetValue(PlayerResourceUI.Value.Close());
        //     CommonCommandUI.SetValue(CommonCommandUI.Value.Close());
        //     ComboCommandUI.SetValue(ComboCommandUI.Value.Close());
        //     BattleCommandUI.SetValue(BattleCommandUI.Value.Close());
        //     SkillCommandUI.SetValue(SkillCommandUI.Value.Close());
        //     DialogueUI.SetValue(DialogueUI.Value.Close());
        //     TradeUI.SetValue(TradeUI.Value.Close());
        //     NotificationUI.SetValue(NotificationUI.Value.Close());
        //     TipUI.SetValue(TipUI.Value.Close());
        // }
    }
}