using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Globalization;
using UnityEngine;

namespace RedScrapCauldron
{
    //This is an example plugin that can be put in BepInEx/plugins/RedScrapCauldron/RedScrapCauldron.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class RedScrapCauldron : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !

        // Token: 0x04000002 RID: 2
        public const string PluginGUID = "com.Pseudo.RedScrapInCauldron";

        // Token: 0x04000003 RID: 3
        public const string PluginName = "Red Scrap for Cauldrons";

        // Token: 0x04000004 RID: 4
        public const string PluginVersion = "1.0.4";

        // Token: 0x06000008 RID: 8 RVA: 0x000020AD File Offset: 0x000002AD
        public void Awake()
        {
            Log.Init(Logger);
            Hooks();
        }

        // Token: 0x06000009 RID: 9 RVA: 0x000020B8 File Offset: 0x000002B8
        public void Hooks()
        {
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += PurchaseInteraction_CanBeAffordedByInteractor;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
            On.RoR2.PurchaseInteraction.GetContextString += PurchaseInteraction_GetContextString;
            On.RoR2.CostHologramContent.FixedUpdate += CostHologramContent_FixedUpdate;
            On.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
        }

        private void SetPriceTo1Red(PurchaseInteraction purchaseInteraction)
        {
            purchaseInteraction.costType = CostTypeIndex.RedItem;
            purchaseInteraction.cost = 1;
        }

        private void SetPriceTo5Green(PurchaseInteraction purchaseInteraction)
        {
            purchaseInteraction.costType = CostTypeIndex.GreenItem;
            purchaseInteraction.cost = 5;
        }

        private bool CheckIfInteractorHasItem(Interactor interactor, ItemDef itemDef)
        {
            return this.CheckIfCharacterMasterHasItem(interactor.GetComponent<CharacterBody>(), itemDef);
        }

        private bool CheckIfCharacterMasterHasItem(CharacterBody characterBody, ItemDef itemDef)
        {
            return characterBody.inventory.GetItemCount(itemDef) > 0;
        }

        private bool PurchaseInteraction_CanBeAffordedByInteractor(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor methodReference, PurchaseInteraction classReference, Interactor interactor)
        {
            bool flag = classReference.costType == CostTypeIndex.GreenItem && classReference.cost == 5 && this.CheckIfInteractorHasItem(interactor, RoR2Content.Items.ScrapRed);
            return flag || methodReference.Invoke(classReference, interactor);
        }

        private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin methodReference, PurchaseInteraction classReference, Interactor interactor)
        {
            bool flag = classReference.costType == CostTypeIndex.GreenItem && classReference.cost == 5 && this.CheckIfInteractorHasItem(interactor, RoR2Content.Items.ScrapRed);
            if (flag)
            {
                this.SetPriceTo1Red(classReference);
                methodReference.Invoke(classReference, interactor);
                this.SetPriceTo5Green(classReference);
            }
            else
            {
                methodReference.Invoke(classReference, interactor);
            }
        }

        private string PurchaseInteraction_GetContextString(On.RoR2.PurchaseInteraction.orig_GetContextString methodReference, PurchaseInteraction classReference, Interactor interactor)
        {
            bool flag = classReference.cost == 5 && classReference.costType == CostTypeIndex.GreenItem;
            string result;
            if (flag)
            {
                PurchaseInteraction.sharedStringBuilder.Clear();
                PurchaseInteraction.sharedStringBuilder.Append(Language.GetString(classReference.contextToken));
                bool flag2 = classReference.costType > CostTypeIndex.None;
                if (flag2)
                {
                    PurchaseInteraction.sharedStringBuilder.Append(" <nobr>(<color=#E7543A>1 Scrap(s)</color>)</nobr> / ");
                    PurchaseInteraction.sharedStringBuilder.Append(" <nobr>(");
                    CostTypeCatalog.GetCostTypeDef(classReference.costType).BuildCostStringStyled(classReference.cost, PurchaseInteraction.sharedStringBuilder, false, true);
                    PurchaseInteraction.sharedStringBuilder.Append(")</nobr>");
                }
                result = PurchaseInteraction.sharedStringBuilder.ToString();
            }
            else
            {
                result = methodReference.Invoke(classReference, interactor);
            }
            return result;
        }

        private void CostHologramContent_FixedUpdate(On.RoR2.CostHologramContent.orig_FixedUpdate methodReference, CostHologramContent classReference)
        {
            bool flag = classReference.displayValue == 5 && classReference.costType == CostTypeIndex.GreenItem;
            if (flag)
            {
                CostHologramContent.sharedStringBuilder.Clear();
                classReference.targetTextMesh.color = Color.white;
                classReference.targetTextMesh.SetText("<nobr><color=#E7543A>1 Scrap(s)</color></nobr><br>OR<br><nobr><color=#77FF17>5 Item(s)</color></nobr>", true);
            }
            else
            {
                methodReference.Invoke(classReference);
            }
        }

        private void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing methodReference, RoR2.UI.PingIndicator classReference)
        {
            classReference.transform.rotation = Util.QuaternionSafeLookRotation(classReference.pingNormal);
            classReference.transform.position = (classReference.pingTarget ? classReference.pingTarget.transform.position : classReference.pingOrigin);
            classReference.transform.localScale = Vector3.one;
            classReference.positionIndicator.targetTransform = (classReference.pingTarget ? classReference.pingTarget.transform : null);
            classReference.positionIndicator.defaultPosition = classReference.transform.position;
            IDisplayNameProvider displayNameProvider = classReference.pingTarget ? classReference.pingTarget.GetComponentInParent<IDisplayNameProvider>() : null;
            ModelLocator modelLocator = null;
            classReference.pingType = RoR2.UI.PingIndicator.PingType.Default;
            classReference.pingObjectScaleCurve.enabled = false;
            classReference.pingObjectScaleCurve.enabled = true;
            GameObject[] array = classReference.defaultPingGameObjects;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].SetActive(false);
            }
            array = classReference.enemyPingGameObjects;
            for (int j = 0; j < array.Length; j++)
            {
                array[j].SetActive(false);
            }
            array = classReference.interactablePingGameObjects;
            for (int k = 0; k < array.Length; k++)
            {
                array[k].SetActive(false);
            }
            bool flag = classReference.pingTarget;
            if (flag)
            {
                Debug.LogFormat("Ping target {0}", new object[]
                {
                    classReference.pingTarget
                });
                modelLocator = classReference.pingTarget.GetComponent<ModelLocator>();
                bool flag2 = displayNameProvider != null;
                if (flag2)
                {
                    CharacterBody component = classReference.pingTarget.GetComponent<CharacterBody>();
                    bool flag3 = component;
                    if (flag3)
                    {
                        classReference.pingType = RoR2.UI.PingIndicator.PingType.Enemy;
                        classReference.targetTransformToFollow = component.coreTransform;
                    }
                    else
                    {
                        classReference.pingType = RoR2.UI.PingIndicator.PingType.Interactable;
                    }
                }
            }
            string bestMasterName = Util.GetBestMasterName(classReference.pingOwner.GetComponent<CharacterMaster>());
            string text = ((MonoBehaviour)displayNameProvider) ? Util.GetBestBodyName(((MonoBehaviour)displayNameProvider).gameObject) : "";
            classReference.pingText.enabled = true;
            classReference.pingText.text = bestMasterName;
            switch (classReference.pingType)
            {
                case RoR2.UI.PingIndicator.PingType.Default:
                    classReference.pingColor = classReference.defaultPingColor;
                    classReference.pingDuration = classReference.defaultPingDuration;
                    classReference.pingHighlight.isOn = false;
                    array = classReference.defaultPingGameObjects;
                    for (int l = 0; l < array.Length; l++)
                    {
                        array[l].SetActive(true);
                    }
                    Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_DEFAULT"), bestMasterName));
                    break;
                case RoR2.UI.PingIndicator.PingType.Enemy:
                    {
                        classReference.pingColor = classReference.enemyPingColor;
                        classReference.pingDuration = classReference.enemyPingDuration;
                        array = classReference.enemyPingGameObjects;
                        for (int m = 0; m < array.Length; m++)
                        {
                            array[m].SetActive(true);
                        }
                        bool flag4 = modelLocator;
                        if (flag4)
                        {
                            Transform modelTransform = modelLocator.modelTransform;
                            bool flag5 = modelTransform;
                            if (flag5)
                            {
                                CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
                                bool flag6 = component2;
                                if (flag6)
                                {
                                    bool flag7 = false;
                                    foreach (CharacterModel.RendererInfo rendererInfo in component2.baseRendererInfos)
                                    {
                                        bool flag8 = !rendererInfo.ignoreOverlays && !flag7;
                                        if (flag8)
                                        {
                                            classReference.pingHighlight.highlightColor = Highlight.HighlightColor.teleporter;
                                            classReference.pingHighlight.targetRenderer = rendererInfo.renderer;
                                            classReference.pingHighlight.strength = 1f;
                                            classReference.pingHighlight.isOn = true;
                                            flag7 = true;
                                        }
                                    }
                                }
                            }
                            Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_ENEMY"), bestMasterName, text));
                        }
                        break;
                    }
                case RoR2.UI.PingIndicator.PingType.Interactable:
                    {
                        classReference.pingColor = classReference.interactablePingColor;
                        classReference.pingDuration = classReference.interactablePingDuration;
                        classReference.pingTargetPurchaseInteraction = classReference.pingTarget.GetComponent<PurchaseInteraction>();
                        Sprite interactableIcon = RoR2.UI.PingIndicator.GetInteractableIcon(classReference.pingTarget);
                        SpriteRenderer component3 = classReference.interactablePingGameObjects[0].GetComponent<SpriteRenderer>();
                        ShopTerminalBehavior component4 = classReference.pingTarget.GetComponent<ShopTerminalBehavior>();
                        bool flag9 = component4;
                        if (flag9)
                        {
                            PickupIndex pickupIndex = component4.CurrentPickupIndex();
                            IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
                            string format = "{0} ({1})";
                            object arg = text;
                            bool flag10 = !component4.pickupIndexIsHidden && component4.pickupDisplay;
                            object arg2;
                            if (flag10)
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
                        else
                        {
                            bool flag11 = !classReference.pingTarget.gameObject.name.Contains("Shrine") && (classReference.pingTarget.GetComponent<GenericPickupController>() || classReference.pingTarget.GetComponent<PickupPickerController>() || classReference.pingTarget.GetComponent<TeleporterInteraction>());
                            if (flag11)
                            {
                                classReference.pingDuration = 60f;
                            }
                        }
                        array = classReference.interactablePingGameObjects;
                        for (int num = 0; num < array.Length; num++)
                        {
                            array[num].SetActive(true);
                        }
                        bool flag12 = modelLocator;
                        Renderer componentInChildren;
                        if (flag12)
                        {
                            componentInChildren = modelLocator.modelTransform.GetComponentInChildren<Renderer>();
                        }
                        else
                        {
                            componentInChildren = classReference.pingTarget.GetComponentInChildren<Renderer>();
                        }
                        bool flag13 = componentInChildren;
                        if (flag13)
                        {
                            classReference.pingHighlight.highlightColor = Highlight.HighlightColor.interactive;
                            classReference.pingHighlight.targetRenderer = componentInChildren;
                            classReference.pingHighlight.strength = 1f;
                            classReference.pingHighlight.isOn = true;
                        }
                        component3.sprite = interactableIcon;
                        bool flag14 = classReference.pingTargetPurchaseInteraction && classReference.pingTargetPurchaseInteraction.costType > CostTypeIndex.None;
                        if (flag14)
                        {
                            RoR2.UI.PingIndicator.sharedStringBuilder.Clear();
                            bool flag15 = classReference.pingTargetPurchaseInteraction.costType == CostTypeIndex.GreenItem && classReference.pingTargetPurchaseInteraction.cost == 5;
                            if (flag15)
                            {
                                RoR2.UI.PingIndicator.sharedStringBuilder.Append("<nobr><color=#E7543A>1 Scrap(s)</color></nobr> / ");
                            }
                            CostTypeCatalog.GetCostTypeDef(classReference.pingTargetPurchaseInteraction.costType).BuildCostStringStyled(classReference.pingTargetPurchaseInteraction.cost, RoR2.UI.PingIndicator.sharedStringBuilder, false, true);
                            Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE_WITH_COST"), bestMasterName, text, RoR2.UI.PingIndicator.sharedStringBuilder.ToString()));
                        }
                        else
                        {
                            Chat.AddMessage(string.Format(Language.GetString("PLAYER_PING_INTERACTABLE"), bestMasterName, text));
                        }
                        break;
                    }
            }
            classReference.pingText.color = classReference.textBaseColor * classReference.pingColor;
            classReference.fixedTimer = classReference.pingDuration;
        }
    }
}
