using System;
using System.Collections;
using System.Collections.Generic;
using Features.Game.Data;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Package;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using VContainer.Unity;

namespace Features.Game.UI.Package
{
    public class GamePackageGridListAdapter : RecyclerViewAdapter
    {
        [SerializeField] private GamePackageGridItemViewHolder itemTemplate;
        [SerializeField] private GamePackageGridPlaceholderViewHolder placeholderTemplate;

        private const int ItemTemplateViewType = 0;
        private const int PlaceholderTemplateViewType = 1;

        [NonSerialized] public IObjectResolver ObjectResolver;
        [NonSerialized] public PackageManager PackageManager;
        [NonSerialized] public GameScene GameScene;

        private readonly List<object> _packages = new();
        
        private System.Action<RecyclerViewHolder, MoveDirection> _navigationMoveCallback;

        protected override IList GetData() => _packages;

        public void Init(System.Action<RecyclerViewHolder, MoveDirection> navigationMoveCallback)
        {
            _navigationMoveCallback = navigationMoveCallback;
        }

        public override int GetItemViewType(int position)
        {
            if (_packages[position] is GamePackageUIData)
            {
                return ItemTemplateViewType;
            }
            else
            {
                return PlaceholderTemplateViewType;
            }
        }

        public override RecyclerViewHolder GetViewHolderTemplate(int viewType) => viewType switch
        {
            ItemTemplateViewType => itemTemplate,
            PlaceholderTemplateViewType => placeholderTemplate,
        };

        protected override RecyclerViewHolder OnCreateViewHolder(int viewType, RecyclerViewHolder viewHolderTemplate)
        {
            var viewHolder = ObjectResolver.Instantiate(viewHolderTemplate);
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Init(PackageManager, GameScene);
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Init();
                }
                    break;
            }

            return viewHolder;
        }

        protected override void OnBindViewHolder(RecyclerViewHolder viewHolder, int position)
        {
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    var itemData = (GamePackageUIData)_packages[position];
                    itemViewHolder.Bind(itemData, _navigationMoveCallback);
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    var placeholderData = (GamePackagePlaceholderUIData)_packages[position];
                    placeholderViewHolder.Bind(placeholderData, _navigationMoveCallback);
                }
                    break;
            }
        }

        protected override void OnShowViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Show();
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Show();
                }
                    break;
            }
        }

        protected override void OnHideViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Hide();
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Hide();
                }
                    break;
            }
        }

        protected override void OnRecycleViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Unbind();
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Unbind();
                }
                    break;
            }
        }

        protected override void OnDestroyViewHolder(RecyclerViewHolder viewHolder)
        {
            switch (viewHolder)
            {
                case GamePackageGridItemViewHolder itemViewHolder:
                {
                    itemViewHolder.Destroy();
                }
                    break;
                case GamePackageGridPlaceholderViewHolder placeholderViewHolder:
                {
                    placeholderViewHolder.Destroy();
                }
                    break;
            }
        }
    }
}