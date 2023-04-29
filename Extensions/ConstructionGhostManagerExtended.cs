using Enums;
using System.Linq;
using UnityEngine;

namespace ModShelter.Extensions
{
    class ConstructionGhostManagerExtended : ConstructionGhostManager
    {
        protected override void Update()
        {
            if (ModShelter.IsModEnabled && ModShelter.InstantBuildEnabled && Input.GetKeyDown(KeyCode.F8))
            {
                foreach (ConstructionGhost m_Unfinished in m_AllGhosts.Where(
                                          m_Ghost => m_Ghost.gameObject.activeSelf
                                                                   && m_Ghost.GetState() != ConstructionGhost.GhostState.Ready))
                {
                    m_Unfinished.SetState(ConstructionGhost.GhostState.Ready);
                }
                UpdateActivity();
            }
            else
            {
                base.Update();
            }
        }

        protected override void UpdateActivity()
        {
            if (ModShelter.IsModEnabled && ModShelter.InstantBuildEnabled)
            {
                if (RelevanceSystem.ENABLED || m_AllGhosts.Count == 0 || Time.time - m_LastUpdateActivityTime < m_UpdateActivityInterval)
                {
                    return;
                }

                float num = Mathf.Min(20, m_AllGhosts.Count);
                for (int i = 0; (float)i < num; i++)
                {
                    if (m_CurrentIndex >= m_AllGhosts.Count)
                    {
                        m_CurrentIndex = 0;
                    }

                    ConstructionGhost constructionGhost = m_AllGhosts[m_CurrentIndex];
                    if (constructionGhost.m_Challenge)
                    {
                        m_CurrentIndex++;
                        continue;
                    }

                    if (ScenarioManager.Get().IsPreDream())
                    {
                        if (constructionGhost.gameObject.activeSelf)
                        {
                            constructionGhost.gameObject.SetActive(value: false);
                        }

                        m_CurrentIndex++;
                        continue;
                    }

                    if (constructionGhost.m_ResultItemID == ItemID.Bamboo_Bridge && GreenHellGame.Instance.m_GHGameMode == GameMode.Story)
                    {
                        if (constructionGhost.gameObject.activeSelf)
                        {
                            constructionGhost.gameObject.SetActive(value: false);
                        }

                        m_CurrentIndex++;
                        continue;
                    }

                    bool flag = VectorExtention.Distance(Player.Get().transform.position, constructionGhost.transform.position) < m_ActivityDist;
                    if (constructionGhost.gameObject.activeSelf != flag && !constructionGhost.IsReady())
                    {
                        constructionGhost.gameObject.SetActive(flag);
                    }

                    m_CurrentIndex++;
                }

                m_LastUpdateActivityTime = Time.time;
            }
            else
            {
                base.UpdateActivity();
            }
        }
    }
}
