using System.ComponentModel;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Trade;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Dialogue.Node
{
    [Name("Trade")]
    [ParadoxNotion.Design.Description("Start the trade between the selected Dialogue Actors.")]
    public class DialogueTradeNode : DTNode
    {
        [SerializeField] private string _sellerActorName;
        [SerializeField] private string _sellerActorParameterID;
        [SerializeField] private string _payerActorName;
        [SerializeField] private string _payerActorParameterID;

        public string SellerActorName
        {
            get
            {
                var result = DLGTree.GetParameterByID(_sellerActorParameterID);
                return result != null ? result.name : _sellerActorName;
            }
            private set
            {
                if (_sellerActorName != value && !string.IsNullOrEmpty(value))
                {
                    _sellerActorName = value;
                    var param = DLGTree.GetParameterByName(value);
                    _sellerActorParameterID = param?.ID;
                }
            }
        }

        public IDialogueActor SellerActor
        {
            get
            {
                var result = DLGTree.GetActorReferenceByID(_sellerActorParameterID);
                return result ?? DLGTree.GetActorReferenceByName(_sellerActorName);
            }
        }

        public string PayerActorName
        {
            get
            {
                var result = DLGTree.GetParameterByID(_payerActorParameterID);
                return result != null ? result.name : _payerActorName;
            }
            private set
            {
                if (_payerActorName != value && !string.IsNullOrEmpty(value))
                {
                    _payerActorName = value;
                    var param = DLGTree.GetParameterByName(value);
                    _payerActorParameterID = param?.ID;
                }
            }
        }

        public IDialogueActor PayerActor
        {
            get
            {
                var result = DLGTree.GetActorReferenceByID(_payerActorParameterID);
                return result ?? DLGTree.GetActorReferenceByName(_payerActorName);
            }
        }

        private TradeManager _tradeManager;

        private TradeManager TradeManager
        {
            get
            {
                if (_tradeManager)
                {
                    return _tradeManager;
                }

                _tradeManager = GameEnvironment.FindEnvironmentComponent<TradeManager>();
                return _tradeManager;
            }
        }

        public override bool requireActorSelection => false;

        protected override Status OnExecute(Component agent, IBlackboard blackboard)
        {
            if (!TradeManager)
            {
                DLGTree.Continue();
                return Status.Success;
            }

            if (SellerActor is not DialogueCharacterActor sellerActor ||
                PayerActor is not DialogueCharacterActor payerActor)
            {
                DLGTree.Continue();
                return Status.Error;
            }

            if (TradeManager.StartTrade(sellerActor.Reference.Value, payerActor.Reference.Value, HandleTradeFinished))
            {
                return Status.Running;
            }
            
            DLGTree.Continue();
            return Status.Failure;
        }

        void HandleTradeFinished()
        {
            status = Status.Success;
            DLGTree.Continue();
        }

#if UNITY_EDITOR
        protected override void OnNodeInspectorGUI()
        {
            GUI.backgroundColor = Colors.lightBlue;
            SellerActorName = EditorUtils.Popup<string>("Seller", SellerActorName, DLGTree.definedActorParameterNames);
            PayerActorName = EditorUtils.Popup<string>("Payer", PayerActorName, DLGTree.definedActorParameterNames);
            GUI.backgroundColor = Color.white;
            base.OnNodeInspectorGUI();
        }

        protected override void OnNodeGUI()
        {
            GUILayout.BeginVertical(Styles.roundedBox);
            GUILayout.Label($"Start trade between {SellerActorName} and {PayerActorName}");
            GUILayout.EndVertical();
        }
#endif
    }
}