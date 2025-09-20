using System;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView.Adapter;
using Framework.Common.UI.RecyclerView.LayoutManager;
using UnityEngine;

namespace Framework.Common.UI.RecyclerView
{
    public class RecyclerViewInitializer: MonoBehaviour
    {
        public enum Stage
        {
            Enable,
            Start,
            Custom
        }
        
        [SerializeField] private RecyclerView recyclerView;
        public RecyclerView RecyclerView => recyclerView;
        
        [SerializeField] private RecyclerViewLayoutManager layoutManager;
        public RecyclerViewLayoutManager LayoutManager => layoutManager;
        
        [SerializeField] private RecyclerViewAdapter adapter;
        public RecyclerViewAdapter Adapter => adapter;
        
        [SerializeField] private Stage initialStage = Stage.Start;

        public event System.Action AfterInit;

        public void Init()
        {
            if (initialStage != Stage.Custom)
            {
                throw new Exception("The initial stage is not Custom");
            }
            
            InitInternal();
        }

        private void OnEnable()
        {
            if (initialStage == Stage.Enable)
            {
                InitInternal();
            }
        }

        private void Start()
        {
            if (initialStage == Stage.Start)
            {
                InitInternal();
            }
        }

        private void InitInternal()
        {
            recyclerView.Init();
            recyclerView.LayoutManager = layoutManager;
            recyclerView.Adapter = adapter;
            AfterInit?.Invoke();
        }

        private void OnValidate()
        {
            if (!recyclerView)
            {
                recyclerView = GetComponent<RecyclerView>();
            }

            if (!layoutManager)
            {
                layoutManager = GetComponent<RecyclerViewLayoutManager>();
            }

            if (!adapter)
            {
                adapter = GetComponent<RecyclerViewAdapter>();
            }
        }
    }
}