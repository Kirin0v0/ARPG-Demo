using System;
using Cinemachine;
using Events;
using Skill.Runtime;
using UnityEngine;

namespace Camera
{
    /// <summary>
    /// 解决CinemachineInputProvider组件附加在Cinemachine相机上，即使被禁用也能接受输入导致相机旋转移动的问题
    /// </summary>
    [RequireComponent(typeof(CinemachineInputProvider))]
    public class CameraInputProvider : MonoBehaviour
    {
        private CinemachineInputProvider _inputProvider;

        private void Awake()
        {
            _inputProvider = GetComponent<CinemachineInputProvider>();
            _inputProvider.enabled = false;
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.CompletePlayerSkill,
                OnCompletePlayerSkill);
        }

        private void OnEnable()
        {
            _inputProvider.enabled = true;
            _inputProvider.AutoEnableInputs = true;
        }

        private void OnDisable()
        {
            _inputProvider.enabled = false;
            _inputProvider.AutoEnableInputs = false;
        }

        private void OnDestroy()
        {
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(GameEvents.CompletePlayerSkill,
                OnCompletePlayerSkill);
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _inputProvider.enabled = false;
            _inputProvider.AutoEnableInputs = false;
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            if (!enabled) return;
            _inputProvider.enabled = true;
            _inputProvider.AutoEnableInputs = true;
        }
    }
}