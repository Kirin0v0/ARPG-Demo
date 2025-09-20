using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character.Ability;
using Common;
using Features.Game.Data;
using Framework.Core.LiveData;
using Player;
using Skill;
using Skill.Runtime;

namespace Features.Game.UI.BattleCommand
{
    public class GameBattleCommandModel
    {
        private const string GroupPageName = "指令";
        private const string AbilityPageName = "固有能力";
        private const string MagicPageName = "动态能力";

        private readonly MutableLiveData<GameBattleCommandPageData> _pageData = new(LiveDataMode.Debounce);
        public LiveData<GameBattleCommandPageData> GetPageData() => _pageData;

        private readonly GameManager _gameManager;
        private readonly SkillManager _skillManager;

        public GameBattleCommandModel(GameManager gameManager, SkillManager skillManager)
        {
            _gameManager = gameManager;
            _skillManager = skillManager;
        }

        public void ShowCollapsePage()
        {
            _pageData.SetValue(new GameBattleCommandCollapsePageData());
        }

        public void SwitchBetweenCollapseAndExpandPage()
        {
            switch (_pageData.Value)
            {
                case GameBattleCommandCollapsePageData collapsePageData:
                {
                    _pageData.SetValue(new GameBattleCommandExpandGroupPageData
                    {
                        Title = GroupPageName,
                        Groups = FetchGroupList(),
                    });
                }
                    break;
                case GameBattleCommandExpandGroupPageData expandGroupPageData:
                {
                    _pageData.SetValue(new GameBattleCommandCollapsePageData());
                }
                    break;
            }
        }

        public void VisiblePage()
        {
            if (_pageData.Value is not GameBattleCommandHiddenPageData)
            {
                return;
            }

            _pageData.SetValue(new GameBattleCommandCollapsePageData());
        }

        public void InvisiblePage()
        {
            _pageData.SetValue(new GameBattleCommandHiddenPageData());
        }

        public void GoToItemPage(GameBattleCommandGroupUIData groupUIData)
        {
            if (_pageData.Value is not GameBattleCommandExpandPageData)
            {
                return;
            }

            _pageData.SetValue(new GameBattleCommandExpandItemPageData
            {
                Title = groupUIData.Name,
                Items = FetchItemList(groupUIData),
            });
        }

        public void BackToGroupPage()
        {
            if (_pageData.Value is not GameBattleCommandExpandPageData)
            {
                return;
            }

            _pageData.SetValue(new GameBattleCommandExpandGroupPageData
            {
                Title = GroupPageName,
                Groups = FetchGroupList(),
            });
        }

        public bool SelectPreviousGroup(int position, out int index, out GameBattleCommandGroupUIData data)
        {
            index = -1;
            data = null;
            if (_pageData.Value is not GameBattleCommandExpandGroupPageData expandGroupPageData || position < 0 ||
                position >= expandGroupPageData.Groups.Count)
            {
                return false;
            }

            index = position - 1 < 0 ? expandGroupPageData.Groups.Count - 1 : position - 1;
            data = expandGroupPageData.Groups[index];
            data.Selected = true;
            return true;
        }

        public bool SelectNextGroup(int position, out int index, out GameBattleCommandGroupUIData data)
        {
            index = -1;
            data = null;
            if (_pageData.Value is not GameBattleCommandExpandGroupPageData expandGroupPageData || position < 0 ||
                position >= expandGroupPageData.Groups.Count)
            {
                return false;
            }

            index = position + 1 >= expandGroupPageData.Groups.Count ? 0 : position + 1;
            data = expandGroupPageData.Groups[index];
            data.Selected = true;
            return true;
        }

        public bool SelectPreviousItem(int position, out int index, out GameBattleCommandItemUIData data)
        {
            index = -1;
            data = null;
            if (_pageData.Value is not GameBattleCommandExpandItemPageData expandItemPageData || position < 0 ||
                position >= expandItemPageData.Items.Count)
            {
                return false;
            }

            index = position - 1 < 0 ? expandItemPageData.Items.Count - 1 : position - 1;
            data = expandItemPageData.Items[index];
            data.Selected = true;
            return true;
        }

        public bool SelectNextItem(int position, out int index, out GameBattleCommandItemUIData data)
        {
            index = -1;
            data = null;
            if (_pageData.Value is not GameBattleCommandExpandItemPageData expandItemPageData || position < 0 ||
                position >= expandItemPageData.Items.Count)
            {
                return false;
            }

            index = position + 1 >= expandItemPageData.Items.Count ? 0 : position + 1;
            data = expandItemPageData.Items[index];
            data.Selected = true;
            return true;
        }

        private List<GameBattleCommandGroupUIData> FetchGroupList()
        {
            var abilitySkills = new List<Skill.Runtime.Skill>();
            var magicSkills = new List<Skill.Runtime.Skill>();
            if (_gameManager.Player)
            {
                _gameManager.Player.Parameters.skills.ForEach(skill =>
                {
                    switch (skill.group)
                    {
                        case SkillGroup.Static:
                        {
                            abilitySkills.Add(skill);
                        }
                            break;
                        case SkillGroup.Dynamic:
                        {
                            magicSkills.Add(skill);
                        }
                            break;
                    }
                });
            }

            var groupList = new List<GameBattleCommandGroupUIData>
            {
                new GameBattleCommandGroupUIData
                {
                    Name = AbilityPageName,
                    Skills = abilitySkills,
                    Enable = abilitySkills.Count != 0 &&
                             _gameManager.Player?.Parameters.control.allowReleaseAbilitySkill == true
                },
                new GameBattleCommandGroupUIData
                {
                    Name = MagicPageName,
                    Skills = magicSkills,
                    Enable = magicSkills.Count != 0 &&
                             _gameManager.Player?.Parameters.control.allowReleaseMagicSkill == true
                }
            };
            return groupList;
        }

        private List<GameBattleCommandItemUIData> FetchItemList(GameBattleCommandGroupUIData groupUIData)
        {
            var itemList = new List<GameBattleCommandItemUIData>();
            groupUIData.Skills.ForEach(skill => { itemList.Add(SkillToItem(skill)); });
            return itemList;

            GameBattleCommandItemUIData SkillToItem(Skill.Runtime.Skill skill)
            {
                return new GameBattleCommandItemUIData
                {
                    SkillGroup = skill.group,
                    Skill = skill,
                    Selected = false,
                };
            }
        }
    }
}