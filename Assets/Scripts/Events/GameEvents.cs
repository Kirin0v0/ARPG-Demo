using Framework.Core.Event;

namespace Events
{
    public static class GameEvents
    {
        public static readonly EventIdentity BeforeGotoScene = new("BeforeGotoScene");
        public static readonly EventIdentity AfterGotoScene = new("AfterGotoScene");
        
        public static readonly EventIdentity Cutscene = new("Cutscene");

        public static readonly EventIdentity Tip = new("Tip");

        public static readonly EventIdentity Teleport = new("Teleport");
        public static readonly EventIdentity RefreshMap = new("RefreshMap");

        public static readonly EventIdentity StartTargetSelection = new("StartTargetSelection");
        public static readonly EventIdentity SelectTarget = new("SelectTarget");
        public static readonly EventIdentity FinishTargetSelection = new("FinishTargetSelection");
        public static readonly EventIdentity CancelTargetSelection = new("CancelTargetSelection");

        public static readonly EventIdentity ReleasePlayerSkill = new("ReleasePlayerSkill");
        public static readonly EventIdentity CompletePlayerSkill = new("CompletePlayerSkill");

        public static readonly EventIdentity EnterDialogue = new("EnterDialogue");
        public static readonly EventIdentity ExitDialogue = new("ExitDialogue");

        public static readonly EventIdentity KillCharacter = new("KillCharacter");
        public static readonly EventIdentity RespawnCharacter = new("RespawnCharacter");
        public static readonly EventIdentity CauseCharacterIntoStunned = new("CauseCharacterIntoStunned");
        public static readonly EventIdentity CauseCharacterExitStunned = new("CauseCharacterExitStunned");
        public static readonly EventIdentity CauseCharacterIntoBroken = new("CauseCharacterIntoBroken");
        public static readonly EventIdentity CauseCharacterExitBroken = new("CauseCharacterExitBroken");

        public static readonly EventIdentity AllowGUIShow = new("AllowGUIShow");
        public static readonly EventIdentity BanGUIShow = new("BanGUIShow");

        public static readonly EventIdentity Notification = new("Notification");
        public static readonly EventIdentity NotificationGet = new("NotificationGet");
        public static readonly EventIdentity NotificationLost = new("NotificationLost");
        public static readonly EventIdentity NotificationNotGetMore = new("NotificationNotGetMore");

        public static readonly EventIdentity TriggerWitchTime = new("TriggerWitchTime");
    }
}