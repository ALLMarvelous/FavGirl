using System;
using HarmonyLib;
using Assets.Scripts.PeroTools.Nice.Interface;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.GameCore.Managers;
using Assets.Scripts.UI.Panels;
using Assets.Scripts.Database;
using UnityEngine;
using Assets.Scripts.PeroTools.Nice.Components;
using UnityEngine.UI;
using Assets.Scripts.UI.GameMain;
using Assets.Scripts.PeroTools.Nice.Actions;
using Assets.Scripts.UI.Controls;
using Assets.Scripts.PeroTools.Nice.Events;
using PeroTools2.Resources;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.PeroTools.Nice.Variables;
using Text = UnityEngine.UI.Text;
using System.Collections.Generic;
using Account;
using Assets.Scripts.PeroTools.UI;
using Assets.Scripts.UI;
using GameLogic;
using PeroPeroGames.GlobalDefines;
using Assets.Scripts.GameCore.GameObjectLogics.ExtraControl;
using Assets.Scripts.UI.Tips;
using UnityEngine.Events;
using Assets.Scripts.UI.Panels.PnlRole;

namespace FavGirl
{
	[Harmony]
	public static class FavManager
    {
		public static PnlStage stagePnl;
		public static Toggle likeBtnGirl;
		public static Toggle likeBtnElfin;
		public static GameObject girlTxt;
		public static GameObject elfinTxt;

		internal static Component CopyComponent(Component original, GameObject destination) {
			Il2CppSystem.Type type = original.GetIl2CppType();
			Component copy = destination.AddComponent(type);
			// Copied fields can be restricted with BindingFlags
			Il2CppSystem.Reflection.FieldInfo[] fields = type.GetFields();
			foreach(Il2CppSystem.Reflection.FieldInfo field in fields) {
				field.SetValue(copy, field.GetValue(original));
			}
			return copy;
		}

		public static bool ValidGirl(int girl) => ValidGirl((GirlID)girl);
		public static bool ValidGirl(GirlID girl) {
			if(girl == GirlID.NONE || !System.Enum.IsDefined(typeof(GirlID), girl)) return false;
			if(CharacterDefine.IsTouhouRole(DataHelper.selectedRoleIndex) && !CharacterDefine.IsTouhouRole((int)girl)) {
				return false;
            }

			foreach(var item in DataHelper.items) {
				if(VariableUtils.GetResult<string>(item["type"].Cast<IVariable>()) != "character") 
					continue;
				else if(VariableUtils.GetResult<int>(item["index"].Cast<IVariable>()) == (int)girl) {
					return VariableUtils.GetResult<bool>(item["isUnlock"].Cast<IVariable>());
				}
            }
			return false;
        }

		public static bool ValidElfin(int elfin) => ValidElfin((ElfinID)elfin);
		public static bool ValidElfin(ElfinID elfin) {
			return elfin != ElfinID.NONE && System.Enum.IsDefined(typeof(ElfinID), elfin);
		}

		public static List<int> _oldGirl = new List<int>();
		public static void PrefixStoreGirlDoThing(bool targetGlobal, System.Action act = null) {
			try {
				var dataID = targetGlobal ? GlobalDataBase.s_DbBattleStage.m_SelectedRole : DataHelper.selectedRoleIndex;
				if(ValidGirl(FavSave.FavGirl) && ValidGirl(dataID)) {
					_oldGirl.Add(dataID);
					if(targetGlobal) {
						GlobalDataBase.s_DbBattleStage.m_SelectedRole = (int)FavSave.FavGirl;
					} else {
						DataHelper.selectedRoleIndex = (int)FavSave.FavGirl;
                    }
				}
				act?.Invoke();
			} catch(Exception e) {
				FavGirlMelon.instance.LoggerInstance.Error(e);
            }
		}
		public static void PostfixRestoreGirlDoThing(bool targetGlobal, System.Action act = null) {
			try {
				var dataID = targetGlobal ? GlobalDataBase.s_DbBattleStage.m_SelectedRole : DataHelper.selectedRoleIndex;
				if(ValidGirl(FavSave.FavGirl) && ValidGirl(dataID)) {
					var girlIdx = _oldGirl[_oldGirl.Count - 1];
					_oldGirl.RemoveAt(_oldGirl.Count - 1);
					if(targetGlobal) {
						GlobalDataBase.s_DbBattleStage.m_SelectedRole = girlIdx;
					} else {
						DataHelper.selectedRoleIndex = girlIdx;
					}
				}
				act?.Invoke();
			} catch(Exception e) {
				FavGirlMelon.instance.LoggerInstance.Error(e);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PnlStage), nameof(PnlStage.PreWarm))]
		private static void PnlStagePreWarmPostfix(PnlStage __instance) {
				stagePnl = __instance;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(FancyScrollView), nameof(FancyScrollView.OnEnable))]
		// Scroll Anim Names: "CharScrollView" "ElfinScrollView"
		private static void FancyScrollViewOnEnablePostfix(FancyScrollView __instance) {
			if(__instance.animName == null) return;

			try {
				if(__instance.animName.Equals("CharScrollView") && likeBtnGirl == null) {
					try {
						var btnGirlObj = UnityEngine.Object.Instantiate(stagePnl.m_TglLikeScript.gameObject, __instance.transform.parent);
						UnityEngine.Object.DestroyImmediate(btnGirlObj.GetComponent<Button>());
						likeBtnGirl = btnGirlObj.AddComponent<Toggle>();
						likeBtnGirl.graphic = btnGirlObj.GetComponent<StageLikeToggle>().m_ImgLike.GetComponent<Image>();
						likeBtnGirl.name = "TglLikeGirl";
						likeBtnGirl.transform.position = new Vector3(-6f, 1.75f, 100f);
						likeBtnGirl.GetComponent<StageLikeToggle>().enabled = false;
						UnityEngine.Object.DestroyImmediate(btnGirlObj.GetComponent<StageLikeToggle>());
						//UnityEngine.Object.Destroy(btnGirlObj.transform.FindChild("ImgLikeHide").gameObject);

						foreach(Il2CppSystem.Object obj in likeBtnGirl.transform) {
							var trans = obj.Cast<Transform>();
							if(trans.name == "ImgHold") trans.gameObject.SetActive(false);
							if(trans.name == "ImgLikeOn") trans.gameObject.SetActive(true);
						}

						PnlRole pnlRole = __instance.transform.parent.GetComponent<PnlRole>();

						likeBtnGirl.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)((val) => {
							if(val) {
								var cell = pnlRole.m_FancyPanel.GetCellComponent<PnlRoleSubControl>(-1);
								if(cell == null || !ValidGirl(cell.m_RoleIndex)) {
									// reimu
									FavGirlMelon.instance.LoggerInstance.Msg("Did you really think you could fool me?");
									FavSave.FavGirl = GirlID.NONE;
								} else {
									var girlIdx = cell.m_RoleIndex;
									FavSave.FavGirl = (GirlID)girlIdx;
								}
							} else {
								FavSave.FavGirl = GirlID.NONE;
							}
						}));

						__instance.onUpdatePosition += (UnityEngine.Events.UnityAction<float>)((val) => {
							var cell = pnlRole.m_FancyPanel.GetCellComponent<PnlRoleSubControl>(-1);
							if(cell != null) {
								var girlIdx = cell.m_RoleIndex;
								if(!ValidGirl(girlIdx)) {
									// reimu or new chars
									likeBtnGirl.gameObject.SetActive(false);
								} else if(!likeBtnGirl.gameObject.activeSelf) {
									likeBtnGirl.gameObject.SetActive(true);
								}
								likeBtnGirl.SetIsOnWithoutNotify((GirlID)girlIdx == FavSave.FavGirl);
							}
						});

                        FavGirlMelon.instance.LoggerInstance.Msg(pnlRole == null ? "NULL PNLROLE" : "");
                        FavGirlMelon.instance.LoggerInstance.Msg(pnlRole.m_FancyPanel == null ? "NULL FANCYPANEL" : "");
                        FavGirlMelon.instance.LoggerInstance.Msg(pnlRole.m_FancyPanel.m_FancyScrollView == null ? "NULL FANCYSCROLLVIEW" : "");
                        FavGirlMelon.instance.LoggerInstance.Msg(pnlRole.m_FancyPanel.m_FancyScrollView.m_CellCount == 0 ? "NO CELLS" : "");
                        var gIdx = pnlRole.m_FancyPanel.GetCellComponent<PnlRoleSubControl>(-1).m_RoleIndex;
						likeBtnGirl.SetIsOnWithoutNotify((GirlID)gIdx == FavSave.FavGirl);

						// These canvases don't make any sense but since UnityExplorer crashes on them I have little choice
						var canvas = likeBtnGirl.gameObject.AddComponent<Canvas>();
						likeBtnGirl.gameObject.AddComponent<GraphicRaycaster>();
						canvas.overrideSorting = true;
						canvas.sortingLayerName = "UI";
						canvas.sortingOrder = 13;

						// Make the back button disable it
						/*var backBtn = FavManager.likeBtnGirl.transform.parent.parent.parent.Find("BtnBack");
						var onClickBack = backBtn.GetComponent<OnClick>();
						var deactivateFavButton = new Deactivate {
							m_Object = new Constance {
								m_Value = new Assets.Scripts.PeroTools.Nice.Values.Object {
									result = FavManager.likeBtnGirl.gameObject
								}.Cast<IValue>()
							}.Cast<IVariable>()
						};
						onClickBack.playables.Insert(0, deactivateFavButton.Cast<IPlayable>());*/
					} catch(Exception e) {
						FavGirlMelon.instance.LoggerInstance.Error("Error initializing PnlRole fav button: " + e);
					}
				} else if(__instance.animName.Equals("CharScrollView") && likeBtnGirl != null) {
					try {
						var pnlRole = __instance.transform.parent.GetComponent<PnlRole>();
						var cell = pnlRole.m_FancyPanel.GetCellComponent<PnlRoleSubControl>(-1);
						likeBtnGirl.gameObject.SetActive(ValidGirl(cell != null ? cell.m_RoleIndex : (int)GirlID.NONE));
					} catch(Exception e) {
						FavGirlMelon.instance.LoggerInstance.Error(e);
					}
				}

				if(__instance.animName.Equals("ElfinScrollView") && likeBtnElfin == null) {
					try {
						var btnElfinObj = UnityEngine.Object.Instantiate(stagePnl.m_TglLikeScript.gameObject, __instance.transform.parent);
						UnityEngine.Object.DestroyImmediate(btnElfinObj.GetComponent<Button>());
						likeBtnElfin = btnElfinObj.AddComponent<Toggle>();
						likeBtnElfin.graphic = btnElfinObj.GetComponent<StageLikeToggle>().m_ImgLike.GetComponent<Image>();
						likeBtnElfin.name = "TglLikeElfin";
						likeBtnElfin.transform.position = new Vector3(-3f, 3.4f, 100f);
						likeBtnElfin.GetComponent<StageLikeToggle>().enabled = false;
						UnityEngine.Object.DestroyImmediate(btnElfinObj.GetComponent<StageLikeToggle>());
						//UnityEngine.Object.Destroy(btnElfinObj.transform.FindChild("ImgLikeHide").gameObject);

						foreach(Il2CppSystem.Object obj in likeBtnElfin.transform) {
							var trans = obj.Cast<Transform>();
							if(trans.name == "ImgHold") trans.gameObject.SetActive(false);
							if(trans.name == "ImgLikeOn") trans.gameObject.SetActive(true);
						}

						likeBtnElfin.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)((val) => {
							if(val) {
								var elfinIdx = __instance.selectItemIndex;
								FavSave.FavElfin = (ElfinID)elfinIdx;
							} else {
								FavSave.FavElfin = ElfinID.NONE;
							}
						}));

						__instance.onUpdatePosition += (UnityEngine.Events.UnityAction<float>)((val) => {
							var elfinIdx = __instance.selectItemIndex;
							if(!ValidElfin(elfinIdx)) {
								// safeguard against new elfins
								likeBtnElfin.gameObject.SetActive(false);
							} else if(!likeBtnElfin.gameObject.activeSelf) {
								likeBtnElfin.gameObject.SetActive(true);
							}
							likeBtnElfin.SetIsOnWithoutNotify((ElfinID)elfinIdx == FavSave.FavElfin);
						});

						var eIdx = (int)Math.Round(__instance.expectCurrentScollPosition);
						likeBtnElfin.SetIsOnWithoutNotify((ElfinID)eIdx == FavSave.FavElfin);

						// Make the back button disable it
						/*var backBtn = FavManager.likeBtnElfin.transform.parent.parent.parent.Find("BtnBack");
						var onClickBack = backBtn.GetComponent<OnClick>();
						var deactivateFavButton = new Deactivate {
							m_Object = new Constance {
								m_Value = new Assets.Scripts.PeroTools.Nice.Values.Object {
									result = FavManager.likeBtnElfin.gameObject
								}.Cast<IValue>()
							}.Cast<IVariable>()
						};
						onClickBack.playables.Insert(0, deactivateFavButton.Cast<IPlayable>());*/
					} catch(Exception e) {
						FavGirlMelon.instance.LoggerInstance.Error("Error initializing PnlElfin fav button: " + e);
					}
				} else if(__instance.animName.Equals("ElfinScrollView")) {
					try {
						var eIdx = (int)Math.Round(__instance.expectCurrentScollPosition);
						likeBtnElfin.SetIsOnWithoutNotify((ElfinID)eIdx == FavSave.FavElfin);
					} catch(Exception e) {
						FavGirlMelon.instance.LoggerInstance.Error(e);
					}
				}
			} catch(Exception e) {
				FavGirlMelon.instance.LoggerInstance.Error(e);
			}
		}
	}

	

	

	/*[HarmonyPatch(typeof(FancyScrollView), "OnDisable")]
	internal class ScrollDisablePatch
    {
		private static void Postfix(FancyScrollView __instance) {
			if(__instance.animName.Equals("CharScrollView")) {
				//FavManager.likeBtnGirl?.gameObject.SetActive(false);
            }
			if(__instance.animName.Equals("ElfinScrollView")) {
				//FavManager.likeBtnElfin?.gameObject.SetActive(false);
			}
		}
    }*/

	[HarmonyPatch(typeof(AbstractGirlManager), nameof(AbstractGirlManager.InstanceGirl))]
	internal class GirlInstancePatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(true);

		private static void Postfix() {
			FavManager.PostfixRestoreGirlDoThing(true, () => {
				if(GlobalDataBase.s_DbBattleStage.m_SelectedRole == (int)GirlID.RIN_SLEEP && FavSave.FavGirl != GirlID.RIN_SLEEP && FavSave.FavGirl != GirlID.NONE) {
					// Apply sleep particles to non-Sleepwalkers using Sleepwalker skill
					try {
						//var sleepGirl = SingletonScriptableObject<ResourcesManager>.instance.LoadFromName<GameObject>("sleepy_girl_battle");
						UnityEngine.Object.Instantiate(SingletonScriptableObject<ResourcesManager>.instance.LoadFromName<GameObject>("fx_sleep_skill"), GlobalManagers.girlManager.girl.transform);
					} catch(Exception e) {
						FavGirlMelon.instance.LoggerInstance.Error(e);
					}
				}
				if(FavSave.FavGirl == GirlID.RIN_SLEEP && GlobalDataBase.s_DbBattleStage.m_SelectedRole != (int)GirlID.RIN_SLEEP) {
					// Remove sleep particles from awake Sleepwalkers
					GlobalManagers.girlManager.girl.transform.GetChild(0).gameObject.SetActive(false);
				}
			});
		}
	}

	[HarmonyPatch(typeof(AbstractGirlManager), nameof(AbstractGirlManager.AwakeInit))]
	internal class GirlInitPatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(true);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(true);
	}

	[HarmonyPatch(typeof(MuseShow), nameof(MuseShow.OnEnable))]
	internal class MuseShowPatch
    {
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(PnlStage), nameof(PnlStage.PlayRandomMusic))]
	internal class RandomMusicPatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(CharCreate), nameof(CharCreate.OnEnable))]
	internal class CharCreatePatch
	{
		private static void Prefix(CharCreate __instance) => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix(CharCreate __instance) => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(PnlVictory), nameof(PnlVictory.OnVictory))]
	internal class OnVictoryPatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(true);
		private static void Postfix(PnlVictory __instance) {
			FavManager.PostfixRestoreGirlDoThing(true, () => {
				var shouldHideDetails = FavSave.conditionalHideScoreDetails.Value && (FavSave.FavGirl == GirlID.NONE || (int)FavSave.FavGirl == DataHelper.selectedRoleIndex || !FavManager.ValidGirl(DataHelper.selectedRoleIndex));
				if(FavManager.girlTxt == null && !shouldHideDetails) {
					var victoryPnl = __instance.m_CurControls.mainPnl;

					var scoreText = __instance.m_CurControls.scoreTxt.transform.parent.gameObject;
					var tittleText = __instance.m_CurControls.accuracyTxt.transform.parent.parent.gameObject;
					var accText = __instance.m_CurControls.accuracyTxt.transform.parent;

					var girlText = UnityEngine.Object.Instantiate(accText, tittleText.transform);
					//var elfinText = UnityEngine.Object.Instantiate(accText, tittleText.transform);
					var buildText = UnityEngine.Object.Instantiate(accText, tittleText.transform);

					girlText.name = "TxtGirl";
					/*girlText.gameObject.GetComponent<Text>().text = "GIRL";
					girlText.gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
					girlText.localPosition = new Vector3(18f, 80f, -2f);
					girlText.GetChild(0).localPosition = new Vector3(girlText.GetChild(0).localPosition.x - 25, girlText.GetChild(0).localPosition.y, girlText.GetChild(0).localPosition.z);
					girlText.GetChild(0).gameObject.GetComponent<OnActivate>().enabled = false;
					girlText.GetChild(0).gameObject.GetComponent<Text>().text = $"{Singleton<ConfigManager>.instance.GetConfigStringValue("character", DataHelper.selectedRoleIndex, "cosName")}";
					girlText.GetChild(0).gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;*/
					FavManager.girlTxt = girlText.gameObject;

					/*elfinText.name = "TxtElfin";
					elfinText.gameObject.GetComponent<Text>().text = "ELFIN";
					elfinText.gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
					elfinText.localPosition = new Vector3(18f, 150f, -2f);
					elfinText.GetChild(0).localPosition = new Vector3(elfinText.GetChild(0).localPosition.x - 25, elfinText.GetChild(0).localPosition.y, elfinText.GetChild(0).localPosition.z);
					elfinText.GetChild(0).gameObject.GetComponent<OnActivate>().enabled = false;
					if(DataHelper.selectedElfinIndex == -1) {
						elfinText.GetChild(0).gameObject.GetComponent<Text>().text = "None";
					} else {
						elfinText.GetChild(0).gameObject.GetComponent<Text>().text = $"{Singleton<ConfigManager>.instance.GetConfigStringValue("elfin", DataHelper.selectedElfinIndex, "name")}";
					}
					elfinText.GetChild(0).gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
					FavManager.elfinTxt = elfinText.gameObject;*/


					buildText.name = "TxtBuild";
					buildText.gameObject.GetComponent<Text>().text = "/";
					UnityEngine.Object.Instantiate(girlText.GetChild(0), buildText.transform);
					girlText.gameObject.SetActive(false);
					buildText.gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
					//buildText.localPosition = new Vector3(408, 356, -2);
					//buildText.Rotate(0f, 0f, 24f);
					//buildText.GetChild(0).gameObject.GetComponent<OnActivate>().enabled = false;
					buildText.GetChild(0).gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
					buildText.GetChild(0).localPosition = new Vector3(-120, buildText.GetChild(0).localPosition.y, buildText.GetChild(0).localPosition.z);
					buildText.GetChild(0).gameObject.GetComponent<Text>().text = $"{Singleton<ConfigManager>.instance.GetConfigStringValue("character", DataHelper.selectedRoleIndex, "cosName")}";
					//buildText.GetChild(1).gameObject.GetComponent<OnActivate>().enabled = false;
					buildText.GetChild(1).gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
					if(DataHelper.selectedElfinIndex == -1) {
						buildText.GetChild(1).gameObject.GetComponent<Text>().text = "None";
					} else {
						buildText.GetChild(1).gameObject.GetComponent<Text>().text = $"{Singleton<ConfigManager>.instance.GetConfigStringValue("elfin", DataHelper.selectedElfinIndex, "name")}";
					}
					buildText.GetChild(1).localPosition = new Vector3(115, buildText.GetChild(1).localPosition.y, buildText.GetChild(1).localPosition.z);
					buildText.localPosition = new Vector3(-20 + buildText.GetChild(0).GetComponent<Text>().preferredWidth, 80f, -2f);
				}
			});
		}
	}

	[HarmonyPatch(typeof(CharVoicePlay), nameof(CharVoicePlay.Execute))]
	internal class CharVoicePlayPatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(CharacterExpression), nameof(CharacterExpression.RefreshExpressions))]
	internal class ExpressionPatchA
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	//[HarmonyPatch(typeof(CharacterExpression), nameof(CharacterExpression.GetCharacterAnimator)]
	internal class ExpressionPatchB
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(CharacterExpression), nameof(CharacterExpression.Express), typeof(Expression), typeof(int))]
	internal class ExpressionPatchC
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(OnActivate), nameof(OnActivate.OnEnable))]
	internal class OnActivatePatch
	{
		private static int oldElfin;

		private static void Prefix(OnActivate __instance) {
			if(__instance.name == "SpinePerfab_other") {
				FavManager.PrefixStoreGirlDoThing(false);
			}
			if(__instance.name == "ElfinShow" && FavSave.FavElfin != ElfinID.NONE) {
				oldElfin = DataHelper.selectedElfinIndex;
				DataHelper.selectedElfinIndex = (int)FavSave.FavElfin;
            }
		}

		private static void Postfix(OnActivate __instance) {
			if(__instance.name == "SpinePerfab_other") {
				FavManager.PostfixRestoreGirlDoThing(false);
			}
			if(__instance.name == "ElfinShow" && FavSave.FavElfin != ElfinID.NONE) {
				DataHelper.selectedElfinIndex = oldElfin;
            }
		}
	}

	/*[HarmonyPatch(typeof(OnClick), "OnExecute")]
	internal class OnClickPatch
	{
		private static void Postfix(OnClick __instance) {
			if(__instance.name == "BtnBack") {
				FavManager.likeBtnGirl?.gameObject.SetActive(false);
				FavManager.likeBtnElfin?.gameObject.SetActive(false);
			}
		}
	}*/

	[HarmonyPatch(typeof(FailBarrage), nameof(FailBarrage.OnEnable))]
	internal class FailBarragePatch
	{
		private static void Prefix() => FavManager.PrefixStoreGirlDoThing(false);
		private static void Postfix() => FavManager.PostfixRestoreGirlDoThing(false);
	}

	[HarmonyPatch(typeof(ElfinCreate), nameof(ElfinCreate.OnBattleStart))]
	internal class ElfinCreatePatch
	{
		public static int oldElfin;

		private static void Prefix() {
			if(FavSave.FavElfin != ElfinID.NONE) {
				FavGirlMelon.instance.LoggerInstance.Msg("Replace Battle Elfin");
				oldElfin = GlobalDataBase.s_DbBattleStage.m_SelectedElfin;
				GlobalDataBase.s_DbBattleStage.m_SelectedElfin = (int)FavSave.FavElfin;
			}
		}

		private static void Postfix() {
			if(FavSave.FavElfin != ElfinID.NONE) {
				GlobalDataBase.s_DbBattleStage.m_SelectedElfin = oldElfin;
			}
		}
	}

	[HarmonyPatch(typeof(StatisticsManager), nameof(StatisticsManager.OnBattleEnd))]
	internal class PPGWasGayHere
    {
		private static void Prefix() {
			if(DataHelper.selectedElfinIndex != GlobalDataBase.s_DbBattleStage.m_SelectedElfin) {
				FavGirlMelon.instance.LoggerInstance.Warning("Battle elfin differs from real elfin, adjusting!");
				GlobalDataBase.s_DbBattleStage.m_SelectedElfin = DataHelper.selectedElfinIndex;
            }
        }
    }

	[HarmonyPatch(typeof(RoleBattleSubControl), nameof(RoleBattleSubControl.Init))]
	internal class RoleBattleSubControlInitPatch
	{
		private static void Postfix(RoleBattleSubControl __instance) {
			// Force normal controller if using favorited Touhou character with non-Touhou skill
			// This happens inside of InstanceGirl, so must grab the latest saved number from the stack
			if(CharacterDefine.IsTouhouRole((int)FavSave.FavGirl) && !CharacterDefine.IsTouhouRole(FavManager._oldGirl[FavManager._oldGirl.Count - 1])) {
				__instance.m_Animator.runtimeAnimatorController = __instance.m_NormalController;
            }
        }
	}

	[HarmonyPatch(typeof(Button), nameof(Button.Press))]
	internal class ButtonPressPatch
    {
		private static void Postfix(Button __instance) {
			if(__instance.name == "BtnApply") {
				if(FavManager.likeBtnGirl != null && !CharacterDefine.IsTouhouRole(DataHelper.selectedRoleIndex)) {
					FavManager.likeBtnGirl.gameObject.SetActive(true);
				}
			}
        }
    }

	[HarmonyPatch(typeof(StageLikeToggle), nameof(StageLikeToggle.OnHideMusic))]
	internal class OnHideMusicPatch
    {
		private static bool Prefix(StageLikeToggle __instance) {
			if(__instance == null || __instance.name != "TglLike") return false;

			return true;
        }
    }

	//[HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.UploadScore))]
	internal class GayCheckingPatch
    {
		private static void Prefix(string musicUid, int musicDifficulty, string characterUid, string elfinUid, int hp, int score, float acc, int maximumCombo, string evaluate, int miss, Il2CppNewtonsoft.Json.Linq.JArray beats, string bmsVersion, Action<int> callback) {
			//FavGirlMelon.instance.LoggerInstance.Msg("SOMETHING GAY: " + ((ElfinID)int.Parse(elfinUid)).ToString());
        }
    }
}
