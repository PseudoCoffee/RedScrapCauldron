using BepInEx;
using RoR2;
using RoR2.UI;
using System;
using System.Globalization;
using System.Security.Permissions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RedScrapCauldron
{
	//This attribute is required, and lists metadata for your plugin.
	[BepInPlugin(PluginGUID, PluginName, PluginVersion)]

	public class Main : BaseUnityPlugin
	{
		// Token: 0x04000002 RID: 2
		public const string PluginGUID = "com.Pseudo.RedScrapInCauldron";

		// Token: 0x04000003 RID: 3
		public const string PluginName = "Red Scrap for Cauldrons";

		// Token: 0x04000004 RID: 4
		public const string PluginVersion = "1.0.7";

		private static string RedColor = "E7543A";
		private static string GreenColor = "77FF17";
		private static string WhiteColor = "FFFFFF";

		public void Awake()
		{
			Log.Init(Logger);
			Hooks();
		}

		public void Hooks()
		{
			On.RoR2.CostTypeDef.IsAffordable += CostTypeDef_IsAffordable;
			On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
			On.RoR2.PurchaseInteraction.GetContextString += PurchaseInteraction_GetContextString;
			On.RoR2.CostHologramContent.FixedUpdate += CostHologramContent_FixedUpdate;
			On.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
		}

		private bool CostTypeDef_IsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef thisReference, int cost, Interactor activator)
		{
			// if it costs 3 whites and user has green scrap
			if (thisReference.itemTier == ItemTier.Tier1 && cost == 3 && (CheckIfInteractorHasItem(activator, RoR2Content.Items.ScrapGreen) || CheckIfInteractorHasItem(activator, DLC1Content.Items.RegeneratingScrap)))
			{
				return true;
			}

			// if it costs 5 greens and user has red scrap
			if (thisReference.itemTier == ItemTier.Tier2 && cost == 5 && CheckIfInteractorHasItem(activator, RoR2Content.Items.ScrapRed))
			{
				return true;
			}

			// default behaviour
			return orig(thisReference, cost, activator);
		}

		private void SetPriceTo(PurchaseInteraction purchaseInteraction, CostTypeIndex type, int cost)
		{
			purchaseInteraction.costType = type;
			purchaseInteraction.cost = cost;
		}

		private bool CheckIfInteractorHasItem(Interactor interactor, ItemDef itemDef)
		{
			return CheckIfCharacterMasterHasItem(interactor.GetComponent<CharacterBody>(), itemDef);
		}

		private bool CheckIfCharacterMasterHasItem(CharacterBody characterBody, ItemDef itemDef)
		{
			return characterBody.inventory.GetItemCount(itemDef) > 0;
		}

		private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin methodReference, PurchaseInteraction thisReference, Interactor interactor)
		{
			// handle 3 white => 1 green
			if (thisReference.costType == CostTypeIndex.WhiteItem && thisReference.cost == 3 && (CheckIfInteractorHasItem(interactor, RoR2Content.Items.ScrapGreen) || CheckIfInteractorHasItem(interactor, DLC1Content.Items.RegeneratingScrap)))
			{
				SetPriceTo(thisReference, CostTypeIndex.GreenItem, 1);
				methodReference.Invoke(thisReference, interactor);
				SetPriceTo(thisReference, CostTypeIndex.WhiteItem, 3);
			}
			// handle 5 green => 1 red
			else if (thisReference.costType == CostTypeIndex.GreenItem && thisReference.cost == 5 && this.CheckIfInteractorHasItem(interactor, RoR2Content.Items.ScrapRed))
			{
				SetPriceTo(thisReference, CostTypeIndex.RedItem, 1);
				methodReference.Invoke(thisReference, interactor);
				SetPriceTo(thisReference, CostTypeIndex.GreenItem, 5);
			}
			else
			{
				methodReference.Invoke(thisReference, interactor);
			}
		}

		private string PurchaseInteraction_GetContextString(On.RoR2.PurchaseInteraction.orig_GetContextString methodReference, PurchaseInteraction thisReference, Interactor interactor)
		{
			string result;
			if ((thisReference.cost == 3 && thisReference.costType == CostTypeIndex.WhiteItem) ||
				(thisReference.cost == 5 && thisReference.costType == CostTypeIndex.GreenItem))
			{
				PurchaseInteraction.sharedStringBuilder.Clear();
				PurchaseInteraction.sharedStringBuilder.Append(Language.GetString(thisReference.contextToken));
				if (thisReference.costType > CostTypeIndex.None)
				{
					string color = thisReference.cost == 5 ? RedColor : GreenColor;
					PurchaseInteraction.sharedStringBuilder.Append($" <nobr>(<color=#{color}>1 Scrap(s)</color>)</nobr> / ");
					PurchaseInteraction.sharedStringBuilder.Append(" <nobr>(");
					CostTypeCatalog.GetCostTypeDef(thisReference.costType).BuildCostStringStyled(thisReference.cost, PurchaseInteraction.sharedStringBuilder, false, true);
					PurchaseInteraction.sharedStringBuilder.Append(")</nobr>");
				}
				result = PurchaseInteraction.sharedStringBuilder.ToString();
			}
			else
			{
				result = methodReference.Invoke(thisReference, interactor);
			}
			return result;
		}

		private void CostHologramContent_FixedUpdate(On.RoR2.CostHologramContent.orig_FixedUpdate methodReference, CostHologramContent thisReference)
		{
			if ((thisReference.displayValue == 3 && thisReference.costType == CostTypeIndex.WhiteItem) ||
				(thisReference.displayValue == 5 && thisReference.costType == CostTypeIndex.GreenItem))
			{
				CostHologramContent.sharedStringBuilder.Clear();
				thisReference.targetTextMesh.color = Color.white;
				string scrapColor = thisReference.costType == CostTypeIndex.GreenItem ? RedColor : GreenColor;
				string itemColor = thisReference.costType == CostTypeIndex.GreenItem ? GreenColor : WhiteColor;
				thisReference.targetTextMesh.SetText($"<nobr><color=#{scrapColor}>1 Scrap(s)</color></nobr><br>OR<br><nobr><color=#{itemColor}>{thisReference.displayValue} Item(s)</color></nobr>", true);
			}
			else
			{
				methodReference.Invoke(thisReference);
			}
		}

		private void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing methodReference, PingIndicator thisReference)
		{
			thisReference.pingHighlight.enabled = false;
			base.transform.rotation = Util.QuaternionSafeLookRotation(thisReference.pingNormal);
			base.transform.position = (thisReference.pingTarget ? thisReference.pingTarget.transform.position : thisReference.pingOrigin);
			base.transform.localScale = Vector3.one;
			thisReference.positionIndicator.targetTransform = (thisReference.pingTarget ? thisReference.pingTarget.transform : null);
			thisReference.positionIndicator.defaultPosition = base.transform.position;
			IDisplayNameProvider displayNameProvider = thisReference.pingTarget ? thisReference.pingTarget.GetComponentInParent<IDisplayNameProvider>() : null;
			ModelLocator modelLocator = null;
			thisReference.pingType = PingIndicator.PingType.Default;
			thisReference.pingObjectScaleCurve.enabled = false;
			thisReference.pingObjectScaleCurve.enabled = true;
			GameObject[] array = thisReference.defaultPingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
			array = thisReference.enemyPingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
			array = thisReference.interactablePingGameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
			if (thisReference.pingTarget)
			{
				Debug.LogFormat("Ping target {0}", new object[]
				{
					thisReference.pingTarget
				});
				modelLocator = thisReference.pingTarget.GetComponent<ModelLocator>();
				if (displayNameProvider != null)
				{
					CharacterBody component = thisReference.pingTarget.GetComponent<CharacterBody>();
					if (component)
					{
						thisReference.pingType = PingIndicator.PingType.Enemy;
						thisReference.targetTransformToFollow = component.coreTransform;
					}
					else
					{
						thisReference.pingType = PingIndicator.PingType.Interactable;
					}
				}
			}
			string ownerName = thisReference.GetOwnerName();
			string text = ((MonoBehaviour)displayNameProvider) ? Util.GetBestBodyName(((MonoBehaviour)displayNameProvider).gameObject) : "";
			//classReference.pingTarget != null;
			thisReference.pingText.enabled = true;
			thisReference.pingText.text = ownerName;
			switch (thisReference.pingType)
			{
				case PingIndicator.PingType.Default:
					thisReference.pingColor = thisReference.defaultPingColor;
					thisReference.pingDuration = thisReference.defaultPingDuration;
					thisReference.pingHighlight.isOn = false;
					array = thisReference.defaultPingGameObjects;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].SetActive(true);
					}
					Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_DEFAULT"), ownerName));
					break;
				case PingIndicator.PingType.Enemy:
					thisReference.pingColor = thisReference.enemyPingColor;
					thisReference.pingDuration = thisReference.enemyPingDuration;
					array = thisReference.enemyPingGameObjects;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].SetActive(true);
					}
					if (modelLocator)
					{
						Transform modelTransform = modelLocator.modelTransform;
						if (modelTransform)
						{
							CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
							if (component2)
							{
								bool flag = false;
								foreach (CharacterModel.RendererInfo rendererInfo in component2.baseRendererInfos)
								{
									if (!rendererInfo.ignoreOverlays && !flag)
									{
										thisReference.pingHighlight.highlightColor = Highlight.HighlightColor.teleporter;
										thisReference.pingHighlight.targetRenderer = rendererInfo.renderer;
										thisReference.pingHighlight.strength = 1f;
										thisReference.pingHighlight.isOn = true;
										thisReference.pingHighlight.enabled = true;
										break;
									}
								}
							}
						}
						Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_ENEMY"), ownerName, text));
					}
					break;
				case PingIndicator.PingType.Interactable:
					{
						thisReference.pingColor = thisReference.interactablePingColor;
						thisReference.pingDuration = thisReference.interactablePingDuration;
						thisReference.pingTargetPurchaseInteraction = thisReference.pingTarget.GetComponent<PurchaseInteraction>();
						thisReference.halcyonShrine = thisReference.pingTarget.GetComponent<HalcyoniteShrineInteractable>();
						Sprite interactableIcon = PingIndicator.GetInteractableIcon(thisReference.pingTarget);
						SpriteRenderer component3 = thisReference.interactablePingGameObjects[0].GetComponent<SpriteRenderer>();
						ShopTerminalBehavior component4 = thisReference.pingTarget.GetComponent<ShopTerminalBehavior>();
						TeleporterInteraction component5 = thisReference.pingTarget.GetComponent<TeleporterInteraction>();
						if (component4)
						{
							PickupIndex pickupIndex = component4.CurrentPickupIndex();
							IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
							string format = "{0} ({1})";
							object arg = text;
							object arg2;
							if (!component4.pickupIndexIsHidden && component4.pickupDisplay)
							{
								PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
								arg2 = Language.GetString(((pickupDef != null) ? pickupDef.nameToken : null) ?? PickupCatalog.invalidPickupToken);
							}
							else
							{
								arg2 = "?";
							}
							text = string.Format(invariantCulture, format, arg, arg2);
						}
						else if (component5)
						{
							thisReference.pingDuration = 30f;
							thisReference.pingText.enabled = false;
							component5.PingTeleporter(ownerName, thisReference);
						}
						else if (!thisReference.pingTarget.gameObject.name.Contains("Shrine") && (thisReference.pingTarget.GetComponent<GenericPickupController>() || thisReference.pingTarget.GetComponent<PickupPickerController>()))
						{
							thisReference.pingDuration = 60f;
						}
						array = thisReference.interactablePingGameObjects;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].SetActive(true);
						}
						Renderer componentInChildren;
						if (modelLocator)
						{
							componentInChildren = modelLocator.modelTransform.GetComponentInChildren<Renderer>();
						}
						else
						{
							componentInChildren = thisReference.pingTarget.GetComponentInChildren<Renderer>();
						}
						if (componentInChildren)
						{
							thisReference.pingHighlight.highlightColor = Highlight.HighlightColor.interactive;
							thisReference.pingHighlight.targetRenderer = componentInChildren;
							thisReference.pingHighlight.strength = 1f;
							thisReference.pingHighlight.isOn = true;
							thisReference.pingHighlight.enabled = true;
						}
						component3.sprite = interactableIcon;
						component3.enabled = !component5;
						if (thisReference.pingTargetPurchaseInteraction && thisReference.pingTargetPurchaseInteraction.costType != CostTypeIndex.None)
						{
							PingIndicator.sharedStringBuilder.Clear();
							if ((thisReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.WhiteItem && thisReference.pingTargetPurchaseInteraction.cost == 3) ||
								(thisReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.GreenItem && thisReference.pingTargetPurchaseInteraction.cost == 5))
							{
								bool isRed = thisReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.GreenItem ? true : false;
								PingIndicator.sharedStringBuilder.Append($"<nobr><color=#{(isRed ? RedColor : GreenColor)}>1 Scrap(s)</color></nobr> / ");
							}
							CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(thisReference.pingTargetPurchaseInteraction.costType);
							int num = thisReference.pingTargetPurchaseInteraction.cost;
							if (thisReference.pingTargetPurchaseInteraction.costType.Equals(CostTypeIndex.Money) && TeamManager.LongstandingSolitudesInParty() > 0)
							{
								num = (int)((float)num * TeamManager.GetLongstandingSolitudeItemCostScale());
							}
							costTypeDef.BuildCostStringStyled(num, PingIndicator.sharedStringBuilder, false, true);
							Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE_WITH_COST"), ownerName, text, PingIndicator.sharedStringBuilder.ToString()));
						}
						else
						{
							Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE"), ownerName, text));
						}
						break;
					}
			}
			thisReference.pingText.color = thisReference.textBaseColor * thisReference.pingColor;
			thisReference.fixedTimer = thisReference.pingDuration;
		}
	}
}
