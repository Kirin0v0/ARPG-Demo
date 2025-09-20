using System;
using System.Collections.Generic;
using Character.Data.Extension;
using Features.Appearance.Data;
using Framework.Core.LiveData;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model.Data;
using Humanoid.Model.Extension;
using Sirenix.Utilities;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Features.Appearance.Model
{
    public class AppearanceModel : IAppearanceModel, IStartable, IDisposable
    {
        private readonly MutableLiveData<HumanoidCharacterRace> _selectedRace = new();
        private readonly MutableLiveData<AppearanceDefaultModelData?> _selectedHair = new();
        private readonly MutableLiveData<AppearanceDefaultModelData?> _selectedHead = new();
        private readonly MutableLiveData<AppearanceDefaultModelData?> _selectedEyebrow = new();
        private readonly MutableLiveData<AppearanceDefaultModelData?> _selectedFacialHair = new();
        private readonly MutableLiveData<HumanoidAppearanceColor> _configurationColor = new();

        private readonly MutableLiveData<HumanoidAppearanceData> _appearance = new();

        private readonly List<AppearanceDefaultModelData> _hairModels = new();
        private readonly List<AppearanceDefaultModelData> _headModels = new();
        private readonly List<AppearanceDefaultModelData> _eyebrowModels = new();
        private readonly List<AppearanceDefaultModelData> _facialHairModels = new();
        private readonly List<AppearanceDefaultModelData> _otherModels = new();

        public void Start()
        {
            _selectedRace.ObserveForever(OnRaceChanged);
            _selectedHair.ObserveForever(OnModelChanged);
            _selectedHead.ObserveForever(OnModelChanged);
            _selectedEyebrow.ObserveForever(OnModelChanged);
            _selectedFacialHair.ObserveForever(OnModelChanged);
            _configurationColor.ObserveForever(OnColorChanged);
        }

        public void Dispose()
        {
            _selectedRace.RemoveObserver(OnRaceChanged);
            _selectedHair.RemoveObserver(OnModelChanged);
            _selectedHead.RemoveObserver(OnModelChanged);
            _selectedEyebrow.RemoveObserver(OnModelChanged);
            _selectedFacialHair.RemoveObserver(OnModelChanged);
            _configurationColor.RemoveObserver(OnColorChanged);
        }

        public LiveData<HumanoidCharacterRace> GetSelectedRace() => _selectedRace;

        public LiveData<AppearanceDefaultModelData?> GetSelectedHair() => _selectedHair;

        public LiveData<AppearanceDefaultModelData?> GetSelectedHead() => _selectedHead;

        public LiveData<AppearanceDefaultModelData?> GetSelectedEyebrow() => _selectedEyebrow;

        public LiveData<AppearanceDefaultModelData?> GetSelectedFacialHair() => _selectedFacialHair;

        public LiveData<HumanoidAppearanceColor> GetConfigurationColor() => _configurationColor;

        public LiveData<HumanoidAppearanceData> GetAppearance() => _appearance;

        public void SelectRace(HumanoidCharacterRace race)
        {
            _selectedRace.SetValue(race);
        }

        public void SelectPreviousHair()
        {
            var hairModel = _selectedHair.Value;
            if (!hairModel.HasValue)
            {
                return;
            }

            var index = _hairModels.IndexOf(hairModel.Value);
            if (index <= 0)
            {
                index = _hairModels.Count - 1;
            }
            else
            {
                index--;
            }

            _selectedHair.SetValue(_hairModels[index]);
        }

        public void SelectNextHair()
        {
            var hairModel = _selectedHair.Value;
            if (!hairModel.HasValue)
            {
                return;
            }

            var index = _hairModels.IndexOf(hairModel.Value);
            if (index >= _hairModels.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            _selectedHair.SetValue(_hairModels[index]);
        }

        public void SelectPreviousHead()
        {
            var headModel = _selectedHead.Value;
            if (!headModel.HasValue)
            {
                return;
            }

            var index = _headModels.IndexOf(headModel.Value);
            if (index <= 0)
            {
                index = _headModels.Count - 1;
            }
            else
            {
                index--;
            }

            _selectedHead.SetValue(_headModels[index]);
        }

        public void SelectNextHead()
        {
            var headModel = _selectedHead.Value;
            if (!headModel.HasValue)
            {
                return;
            }

            var index = _headModels.IndexOf(headModel.Value);
            if (index >= _headModels.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            _selectedHead.SetValue(_headModels[index]);
        }

        public void SelectPreviousEyebrow()
        {
            var eyebrowModel = _selectedEyebrow.Value;
            if (!eyebrowModel.HasValue)
            {
                return;
            }

            var index = _eyebrowModels.IndexOf(eyebrowModel.Value);
            if (index <= 0)
            {
                index = _eyebrowModels.Count - 1;
            }
            else
            {
                index--;
            }

            _selectedEyebrow.SetValue(_eyebrowModels[index]);
        }

        public void SelectNextEyebrow()
        {
            var eyebrowModel = _selectedEyebrow.Value;
            if (!eyebrowModel.HasValue)
            {
                return;
            }

            var index = _eyebrowModels.IndexOf(eyebrowModel.Value);
            if (index >= _eyebrowModels.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            _selectedEyebrow.SetValue(_eyebrowModels[index]);
        }

        public void SelectPreviousFacialHair()
        {
            var facialHairModel = _selectedFacialHair.Value;
            if (!facialHairModel.HasValue)
            {
                return;
            }

            var index = _facialHairModels.IndexOf(facialHairModel.Value);
            if (index <= 0)
            {
                index = _facialHairModels.Count - 1;
            }
            else
            {
                index--;
            }

            _selectedFacialHair.SetValue(_facialHairModels[index]);
        }

        public void SelectNextFacialHair()
        {
            var facialHairModel = _selectedFacialHair.Value;
            if (!facialHairModel.HasValue)
            {
                return;
            }

            var index = _facialHairModels.IndexOf(facialHairModel.Value);
            if (index >= _facialHairModels.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            _selectedFacialHair.SetValue(_facialHairModels[index]);
        }

        public void SetConfigurationColor(HumanoidAppearanceColor color)
        {
            _configurationColor.SetValue(color);
        }

        public void RandomHeadPart()
        {
            if (_hairModels.Count != 0)
            {
                _selectedHair.SetValue(_hairModels[Random.Range(0, _hairModels.Count)]);
            }

            if (_headModels.Count != 0)
            {
                _selectedHead.SetValue(_headModels[Random.Range(0, _headModels.Count)]);
            }

            if (_eyebrowModels.Count != 0)
            {
                _selectedEyebrow.SetValue(_eyebrowModels[Random.Range(0, _eyebrowModels.Count)]);
            }

            if (_facialHairModels.Count != 0)
            {
                _selectedFacialHair.SetValue(_facialHairModels[Random.Range(0, _facialHairModels.Count)]);
            }

            _configurationColor.SetValue(_configurationColor.Value.Clone(
                hairColor: Random.ColorHSV(),
                stubbleColor: Random.ColorHSV(),
                eyesColor: Random.ColorHSV(),
                scarColor: Random.ColorHSV()
            ));
        }

        public void RandomBodyPart()
        {
            _configurationColor.SetValue(_configurationColor.Value.Clone(
                skinColor: Random.ColorHSV(),
                bodyArtColor: Random.ColorHSV(),
                primaryColor: Random.ColorHSV(),
                secondaryColor: Random.ColorHSV(),
                metalPrimaryColor: Random.ColorHSV(),
                metalSecondaryColor: Random.ColorHSV(),
                metalDarkColor: Random.ColorHSV(),
                leatherPrimaryColor: Random.ColorHSV(),
                leatherSecondaryColor: Random.ColorHSV()
            ));
        }

        private void OnRaceChanged(HumanoidCharacterRace race)
        {
            // 获取默认模型数据
            var modelInfoContainer =
                GameApplication.Instance.ExcelBinaryManager.GetContainer<HumanoidModelInfoContainer>();
            var defaultModelInfoContainer = GameApplication.Instance.ExcelBinaryManager
                .GetContainer<AppearanceDefaultModelInfoContainer>();
            var defaultAppearanceModels = new List<AppearanceDefaultModelData>();
            defaultModelInfoContainer.Data.Values.ForEach(defaultModelInfo =>
            {
                if (modelInfoContainer.Data.TryGetValue(defaultModelInfo.Id, out var modelInfo))
                {
                    defaultAppearanceModels.Add(new AppearanceDefaultModelData
                    {
                        ModelInfo = modelInfo,
                        Alias = defaultModelInfo.Alias,
                        Type = defaultModelInfo.Type switch
                        {
                            "hair" => AppearanceModelType.Hair,
                            "head" => AppearanceModelType.Head,
                            "eyebrow" => AppearanceModelType.Eyebrow,
                            "facialHair" => AppearanceModelType.FacialHair,
                            _ => AppearanceModelType.Others,
                        }
                    });
                }
            });

            // 更新与种族匹配的模型列表
            _hairModels.Clear();
            _headModels.Clear();
            _eyebrowModels.Clear();
            _facialHairModels.Clear();
            _otherModels.Clear();
            defaultAppearanceModels.ForEach(modelData =>
            {
                var modelRecord = modelData.ModelInfo.ToModelRecord();
                if (modelRecord.Type == HumanoidModelType.Gear ||
                    !race.ToAppearanceRace().MatchGenderRestriction(modelRecord.GenderRestriction))
                {
                    return;
                }

                switch (modelData.Type)
                {
                    case AppearanceModelType.Hair:
                    {
                        _hairModels.Add(modelData);
                    }
                        break;
                    case AppearanceModelType.Head:
                    {
                        _headModels.Add(modelData);
                    }
                        break;
                    case AppearanceModelType.Eyebrow:
                    {
                        _eyebrowModels.Add(modelData);
                    }
                        break;
                    case AppearanceModelType.FacialHair:
                    {
                        _facialHairModels.Add(modelData);
                    }
                        break;
                    case AppearanceModelType.Others:
                    {
                        _otherModels.Add(modelData);
                    }
                        break;
                }
            });

            #region 重新选择模型

            if (_hairModels.Count == 0)
            {
                _selectedHair.SetValue(null);
            }
            else
            {
                _selectedHair.SetValue(_hairModels[0]);
            }

            if (_headModels.Count == 0)
            {
                _selectedHead.SetValue(null);
            }
            else
            {
                _selectedHead.SetValue(_headModels[0]);
            }

            if (_eyebrowModels.Count == 0)
            {
                _selectedEyebrow.SetValue(null);
            }
            else
            {
                _selectedEyebrow.SetValue(_eyebrowModels[0]);
            }

            if (_facialHairModels.Count == 0)
            {
                _selectedFacialHair.SetValue(null);
            }
            else
            {
                _selectedFacialHair.SetValue(_facialHairModels[0]);
            }

            #endregion

            RefreshAppearance();
        }

        private void OnModelChanged(AppearanceDefaultModelData? data)
        {
            RefreshAppearance();
        }

        private void OnColorChanged(HumanoidAppearanceColor color)
        {
            RefreshAppearance();
        }

        private void RefreshAppearance()
        {
            var appearanceModels = new List<HumanoidAppearanceModel>();
            if (_selectedHair.Value.HasValue)
            {
                appearanceModels.Add(ToAppearanceModel(_selectedHair.Value.Value));
            }

            if (_selectedHead.Value.HasValue)
            {
                appearanceModels.Add(ToAppearanceModel(_selectedHead.Value.Value));
            }

            if (_selectedEyebrow.Value.HasValue)
            {
                appearanceModels.Add(ToAppearanceModel(_selectedEyebrow.Value.Value));
            }

            if (_selectedFacialHair.Value.HasValue)
            {
                appearanceModels.Add(ToAppearanceModel(_selectedFacialHair.Value.Value));
            }

            _otherModels.ForEach(modelData => appearanceModels.Add(ToAppearanceModel(modelData)));

            _appearance.SetValue(HumanoidAppearanceData.JustBody(
                new HumanoidBodyAppearance
                {
                    Models = appearanceModels,
                    Color = _configurationColor.Value,
                })
            );
        }

        private HumanoidAppearanceModel ToAppearanceModel(AppearanceDefaultModelData defaultModelData)
        {
            var modelRecord = defaultModelData.ModelInfo.ToModelRecord();
            return new HumanoidAppearanceModel
            {
                Id = defaultModelData.ModelInfo.Id,
                Part = modelRecord.Part,
                GenderRestriction = modelRecord.GenderRestriction,
                Name = modelRecord.Name,
            };
        }
    }
}