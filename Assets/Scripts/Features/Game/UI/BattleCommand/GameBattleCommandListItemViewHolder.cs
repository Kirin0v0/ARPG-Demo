using System;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.Toast;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.Game.UI.BattleCommand
{
    [RequireComponent(typeof(RecyclerViewHolderSelectable))]
    public class GameBattleCommandListItemViewHolder : RecyclerViewHolder
    {
        [SerializeField] private TextMeshProUGUI textName;
        [SerializeField] private TextMeshProUGUI textCost;
        [SerializeField] private Image imgBackgroundEnable;
        [SerializeField] private Image imgBackgroundDisable;

        private RecyclerViewHolderSelectable _selectable;
        public GameBattleCommandItemUIData Data { get; private set; }
        private PlayerCharacterObject _player;
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;
        private System.Action<object> _clickCallback;

        public void Init()
        {
            _selectable = GetComponent<RecyclerViewHolderSelectable>();
            _selectable.OnNavigationSelect += OnSelect;
            _selectable.OnNavigationDeselect += OnDeselect;
            _selectable.OnNavigationMove += OnMove;
            _selectable.onClick.AddListener(ClickItem);
        }

        public void Bind(
            GameBattleCommandItemUIData itemUIData,
            PlayerCharacterObject player,
            System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback,
            System.Action<object> clickCallback
        )
        {
            Data = itemUIData;
            _player = player;
            _navigationMoveCallback = navigationMoveCallback;
            _clickCallback = clickCallback;

            textName.text = itemUIData.Name;
            textCost.text = itemUIData.Cost;
            UpdateSkillEnable();
        }

        public void Show()
        {
            if (Data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }
        }

        private void Update()
        {
            if (Data != null)
            {
                UpdateSkillEnable();
            }
        }

        private void UpdateSkillEnable()
        {
            if (_player && _player.SkillAbility.MatchSkillPreconditions(Data.Skill.id, Data.SkillGroup, out _))
            {
                imgBackgroundEnable.gameObject.SetActive(true);
                imgBackgroundDisable.gameObject.SetActive(false);
            }
            else
            {
                imgBackgroundEnable.gameObject.SetActive(false);
                imgBackgroundDisable.gameObject.SetActive(true);
            }
        }

        private void ClickItem()
        {
            _clickCallback?.Invoke(Data);
        }

        public void Hide()
        {
            if (Data.Selected)
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        public void Unbind()
        {
            Data = null;
            _player = null;
            _navigationMoveCallback = null;
            _clickCallback = null;
        }

        public void Destroy()
        {
            _selectable.OnNavigationSelect -= OnSelect;
            _selectable.OnNavigationDeselect -= OnDeselect;
            _selectable.OnNavigationMove -= OnMove;
            _selectable.onClick.RemoveListener(ClickItem);
            _selectable = null;
        }

        private void OnSelect()
        {
            if (Data != null)
            {
                Data.Selected = true;
            }
        }

        private void OnDeselect()
        {
            if (Data != null)
            {
                Data.Selected = false;
            }
        }

        private void OnMove(MoveDirection moveDirection)
        {
            _navigationMoveCallback?.Invoke(this, moveDirection);
        }
    }
}