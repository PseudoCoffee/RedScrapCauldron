using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using System.Security.Permissions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RedScrapCauldron
{
	//This attribute is required, and lists metadata for your plugin.
	[BepInEx.BepInPlugin(PluginGUID, PluginName, PluginVersion)]

	public class Main : BepInEx.BaseUnityPlugin
	{
		// Token: 0x04000002 RID: 2
		public const string PluginGUID = "com.Pseudo.RedScrapInCauldron";

		// Token: 0x04000003 RID: 3
		public const string PluginName = "Red Scrap for Cauldrons";

		// Token: 0x04000004 RID: 4
		public const string PluginVersion = "1.0.8";

		private const string RedColor = "E7543A";
		private const string GreenColor = "77FF17";
		private const string WhiteColor = "FFFFFF";

		public static ConfigEntry<bool> DisableUIChanges { get; set; }

		public void Awake()
		{
			Log.Init(Logger);
			ConfigSetup();
			Hooks();
		}

		private void ConfigSetup()
		{
			DisableUIChanges = Config.Bind(section: "General", key: "Disable UI changes", defaultValue: false, configDescription: new ConfigDescription("Will make pings and UI elements not show up for the price changes but may fix compatibility issues with other mods."));
			ModSettingsManager.AddOption(new CheckBoxOption(DisableUIChanges, restartRequired: false));
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
			return characterBody.inventory.GetItemCountPermanent(itemDef) > 0;
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
			else if (thisReference.costType == CostTypeIndex.GreenItem && thisReference.cost == 5 && CheckIfInteractorHasItem(interactor, RoR2Content.Items.ScrapRed))
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

		private bool Costs3White(PurchaseInteraction purchaseInteraction)
		{
			return purchaseInteraction.cost == 3 && purchaseInteraction.costType == CostTypeIndex.WhiteItem;
		}

		private bool Costs3White(CostHologramContent costHologramContent)
		{
			return costHologramContent.displayValue == 3 && costHologramContent.costType == CostTypeIndex.WhiteItem;
		}

		private bool Costs5Green(PurchaseInteraction purchaseInteraction)
		{
			return purchaseInteraction.cost == 5 && purchaseInteraction.costType == CostTypeIndex.GreenItem;
		}

		private bool Costs5Green(CostHologramContent costHologramContent)
		{
			return costHologramContent.displayValue == 5 && costHologramContent.costType == CostTypeIndex.GreenItem;
		}

		// maybe text in chat or near buttons
		private string PurchaseInteraction_GetContextString(On.RoR2.PurchaseInteraction.orig_GetContextString methodReference, PurchaseInteraction thisReference, Interactor interactor)
		{
			string result;
			if (!DisableUIChanges.Value && (Costs3White(thisReference) || Costs5Green(thisReference)))
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

		// hologram text
		private void CostHologramContent_FixedUpdate(On.RoR2.CostHologramContent.orig_FixedUpdate methodReference, CostHologramContent thisReference)
		{
			if (!DisableUIChanges.Value && (Costs3White(thisReference) || Costs5Green(thisReference)))
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

		// show correct text when pinged
		public void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing methodReference, PingIndicator thisReference)
		{
			if (DisableUIChanges.Value)
			{
				methodReference(thisReference);
				return;
			}

			thisReference.pingTargetRenderers.Clear();
			thisReference.transform.rotation = Util.QuaternionSafeLookRotation(thisReference.pingNormal);
			thisReference.transform.localScale = Vector3.one;
			if (thisReference.weakPointTarget)
			{
				Vector3 position = thisReference.weakPointTarget.transform.position;
				thisReference.transform.position = position;
				thisReference.positionIndicator.targetTransform = thisReference.weakPointTarget.transform;
				thisReference.positionIndicator.defaultPosition = position;
			}
			else
			{
				thisReference.transform.position = ((thisReference.pingTarget && !thisReference.forceUseHitOriginForPosition) ? thisReference.pingTarget.transform.position : thisReference.pingOrigin);
				thisReference.positionIndicator.targetTransform = ((thisReference.pingTarget && !thisReference.forceUseHitOriginForPosition) ? thisReference.pingTarget.transform : null);
				thisReference.positionIndicator.defaultPosition = thisReference.transform.position;
			}
			thisReference.positionIndicator.ReCalcHeadOffset(thisReference.positionIndicator.targetTransform);
			IDisplayNameProvider? displayNameProvider = thisReference.pingTarget ? thisReference.pingTarget.GetComponentInParent<IDisplayNameProvider>() : null;
			ModelLocator? modelLocator = null;
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
					if (!thisReference.pingTarget.GetComponent<PurchaseInteraction>() && component)
					{
						thisReference.pingType = PingIndicator.PingType.Enemy;
						thisReference.targetTransformToFollow = component.coreTransform;
					}
					else
					{
						thisReference.targetTransformToFollow = thisReference.pingTarget.transform;
						thisReference.pingType = PingIndicator.PingType.Interactable;
					}
				}
			}
			string ownerName = thisReference.GetOwnerName();
			string text = ((MonoBehaviour?)displayNameProvider) ? Util.GetBestBodyName(((MonoBehaviour?)displayNameProvider)!.gameObject) : "";
			//thisReference.pingTarget != null;
			thisReference.pingText.enabled = true;
			thisReference.pingText.text = ownerName;
			switch (thisReference.pingType)
			{
				case PingIndicator.PingType.Default:
					thisReference.pingColor = thisReference.defaultPingColor;
					thisReference.pingDuration = thisReference.defaultPingDuration;
					thisReference.pingTargetRenderers.Clear();
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
						Transform? modelTransform = modelLocator?.modelTransform;
						if (modelTransform)
						{
							CharacterModel? component2 = modelTransform?.GetComponent<CharacterModel>();
							if (component2)
							{
								foreach (CharacterModel.RendererInfo? rendererInfo in component2!.baseRendererInfos)
								{
									if (rendererInfo.HasValue && !rendererInfo.Value.ignoreOverlays && rendererInfo.Value.renderer.gameObject.activeInHierarchy)
									{
										thisReference.pingTargetRenderers.Add(rendererInfo.Value.renderer);
										thisReference.activeColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Teleporter);
										if (!component2.fullBodyPing)
										{
											break;
										}
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
						DroneVendorTerminalBehavior component14 = thisReference.pingTarget.GetComponent<DroneVendorTerminalBehavior>();
						ShopTerminalBehavior component5 = thisReference.pingTarget.GetComponent<ShopTerminalBehavior>();
						PickupDistributorBehavior component6 = thisReference.pingTarget.GetComponent<PickupDistributorBehavior>();
						TeleporterInteraction component7 = thisReference.pingTarget.GetComponent<TeleporterInteraction>();
						if (component14)
						{
							PickupIndex currentPickupIndex = component14.CurrentPickupIndex;
							bool shouldShowName = !component14.pickupIndexIsHidden && component14.pickupDisplay;
							text = PingIndicator.GetFormattedTargetString(text, currentPickupIndex, shouldShowName);
						}
						else if (component5 && !component5.ignorePingFormatting)
						{
							UniquePickup pickup = component5.CurrentPickup();
							bool shouldShowName2 = !component5.pickupIndexIsHidden && component5.pickupDisplay;
							text = PingIndicator.GetFormattedTargetString(text, pickup.pickupIndex, shouldShowName2);
						}
						else if (component6)
						{
							UniquePickup pickup = component6.GetPickup();
							bool shouldShowName3 = !component6.pickupIndexIsHidden;
							text = PingIndicator.GetFormattedTargetString(text, pickup.pickupIndex, shouldShowName3);
						}
						else if (component7)
						{
							thisReference.pingDuration = 30f;
							thisReference.pingText.enabled = false;
							component7.PingTeleporter(ownerName, thisReference);
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
						Renderer? componentInChildren;
						if (modelLocator)
						{
							componentInChildren = modelLocator?.modelTransform?.GetComponentInChildren<Renderer>();
						}
						else
						{
							componentInChildren = thisReference.pingTarget.GetComponentInChildren<Renderer>();
						}
						if (componentInChildren)
						{
							thisReference.pingTargetRenderers.Add(componentInChildren);
							thisReference.activeColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Interactable);
						}
						component3.sprite = interactableIcon;
						component3.enabled = !component7;
						if (thisReference.pingTargetPurchaseInteraction && thisReference.pingTargetPurchaseInteraction.costType != CostTypeIndex.None)
						{
							PingIndicator.sharedStringBuilder.Clear();
							if ((thisReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.WhiteItem && thisReference.pingTargetPurchaseInteraction.cost == 3) || (thisReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.GreenItem && thisReference.pingTargetPurchaseInteraction.cost == 5))
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
