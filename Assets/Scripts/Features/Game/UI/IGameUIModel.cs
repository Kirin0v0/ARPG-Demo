using Features.Game.Data;
using Framework.Core.LiveData;

namespace Features.Game.UI
{
    public interface IGameUIModel
    {
        #region 系统UI
        
        LiveData<GameUIData> GetLoadingUI();
        LiveData<GameLoadingUIData> GetLoadingData();
        LiveData<GameUIData> GetCutsceneUI();
        LiveData<GameUIData> GetMenuUI();
        LiveData<GameUIData> GetMapUI();
        LiveData<GameUIData> GetCharacterUI();
        LiveData<GameUIData> GetPackageUI();
        LiveData<GameUIData> GetQuestUI();
        LiveData<GameUIData> GetArchiveUI();
        LiveData<GameUIData> GetDeathUI();

        #endregion

        #region 普通UI

        LiveData<GameUIData> GetSystemCommandUI();
        LiveData<GameUIData> GetCommonCommandUI();
        LiveData<GameUIData> GetComboCommandUI();
        LiveData<GameUIData> GetBattleCommandUI();
        LiveData<GameUIData> GetSkillCommandUI();
        LiveData<GameUIData> GetPlayerResourceUI();
        LiveData<GameUIData> GetMiniMapUI();
        LiveData<GameUIData> GetDialogueUI();
        LiveData<GameUIData> GetTradeUI();
        LiveData<GameUIData> GetNotificationUI();
        LiveData<GameUIData> GetTipUI();

        #endregion

        LiveData<bool> HidePlaceholderUI();
        
        LiveData<bool> IsBattleCommandExpanding();
        LiveData<bool> IsDialogueShowing();
        
        LiveData<bool> AllowGUIShowing();
        LiveData<bool> AllowCharacterInformationShowing();
        LiveData<bool> AllowOutlineShowing();
    }
}