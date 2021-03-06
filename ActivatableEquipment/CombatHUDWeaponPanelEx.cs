﻿using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomComponents;
using Harmony;
using HBS;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomActivatableEquipment {
  public class WeaponSlotsLabelsToggle : EventTrigger {
    private bool hovered = false;
    private bool inited = false;
    public LocalizableText text_WeaponText { get; set; }
    public LocalizableText text_Ammo { get; set; }
    public LocalizableText text_Damage { get; set; }
    public LocalizableText text_HitChance { get; set; }
    public void Init(LocalizableText wt, LocalizableText at, LocalizableText dt, LocalizableText ht) {
      text_WeaponText = wt;
      text_Ammo = at;
      text_Damage = dt;
      text_HitChance = ht;
    }
    public override void OnPointerEnter(PointerEventData data) {
      Log.LogWrite("WeaponSlotsLabelsToggle.OnPointerEnter called." + data.position + "\n");
      if (text_WeaponText != null) { text_WeaponText.color = Color.white; };
      if (text_Ammo != null) { text_Ammo.color = Color.white; };
      if (text_Damage != null) { text_Damage.color = Color.white; };
      if (text_HitChance != null) { text_HitChance.color = Color.white; };
      hovered = true;
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.LogWrite("WeaponSlotsLabelsToggle.OnPointerExit called." + data.position + "\n");
      if (text_WeaponText != null) { text_WeaponText.color = Color.grey; };
      if (text_Ammo != null) { text_Ammo.color = Color.grey; };
      if (text_Damage != null) { text_Damage.color = Color.grey; };
      if (text_HitChance != null) { text_HitChance.color = Color.grey; };
      hovered = false;
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.LogWrite("WeaponSlotsLabelsToggle.OnPointerClick called." + data.position + "\n");
      if (this.hovered) if (CombatHUDWeaponPanelExHelper.panelEx != null) CombatHUDWeaponPanelExHelper.panelEx.Toggle();
      //line.SetText(dataline.ToString(false));
    }
  }
  public class CombatHUDEquipmentPanel : EventTrigger {
    public static CombatHUDEquipmentPanel Instance { get; set; } = null;
    public List<CombatHUDEquipmentSlotEx> slots;
    private Dictionary<CombatHUDSidePanelHoverElement, CombatHUDEquipmentSlotEx> clickReceivers = new Dictionary<CombatHUDSidePanelHoverElement, CombatHUDEquipmentSlotEx>();
    public CombatHUD HUD { get; private set; }
    public CombatHUDWeaponPanel weaponPanel { get; private set; }
    public LocalizableText label_WeaponText { get; set; }
    public LocalizableText label_State { get; set; }
    public LocalizableText label_Charges { get; set; }
    public LocalizableText label_FailChance { get; set; }
    private List<CombatHUDEquipmentSlotEx> operatinalSlots { get; set; } = new List<CombatHUDEquipmentSlotEx>();
    public float TimeToLoad = 0.3f;
    public float TimeToUnload = 0.15f;
    private float timeInCurrentState;
    private WPState state { get; set; }
    public void Hide() {
      foreach (CombatHUDEquipmentSlotEx slot in operatinalSlots) {
        slot.Hide();
      }
    }
    public void Show() {
      foreach (CombatHUDEquipmentSlotEx slot in operatinalSlots) {
        slot.RealState = true;
        slot.RefreshComponent();
      }
    }
    public void Toggle() {
      switch (state) {
        case WPState.Loaded:
        case WPState.Loading:
          timeInCurrentState = 0f;
          state = WPState.Unloading;
          break;
        case WPState.Off:
        case WPState.Unloading:
          timeInCurrentState = 0f;
          state = WPState.Loading;
          break;
      }
    }
    public void ProcessOnPointerClick(CombatHUDSidePanelHoverElement hover) {
      if (clickReceivers.ContainsKey(hover)) { clickReceivers[hover].OnPointerClick(null); }
    }
    public override void OnPointerEnter(PointerEventData data) {
      Log.LogWrite("CombatHUDEquipmentPanel.OnPointerEnter called." + data.position + "\n");
      if (label_WeaponText != null) { label_WeaponText.color = Color.white; };
      if (label_State != null) { label_State.color = Color.white; };
      if (label_Charges != null) { label_Charges.color = Color.white; };
      if (label_FailChance != null) { label_FailChance.color = Color.white; };
    }
    public override void OnPointerExit(PointerEventData data) {
      Log.LogWrite("CombatHUDEquipmentPanel.OnPointerExit called." + data.position + "\n");
      if (label_WeaponText != null) { label_WeaponText.color = Color.grey; };
      if (label_State != null) { label_State.color = Color.grey; };
      if (label_Charges != null) { label_Charges.color = Color.grey; };
      if (label_FailChance != null) { label_FailChance.color = Color.grey; };
    }
    public override void OnPointerClick(PointerEventData data) {
      Log.LogWrite("CombatHUDEquipmentPanel.OnPointerClick called." + data.position + "\n");
      Toggle();
      //if (CombatHUDWeaponPanelExHelper.panelEx != null) CombatHUDWeaponPanelExHelper.panelEx.Toggle();
      //line.SetText(dataline.ToString(false));
    }
    private static void DestroyRec(GameObject obj) {
      foreach (Transform child in obj.transform) {
        CombatHUDEquipmentPanel.DestroyRec(child.gameObject);
      }
      GameObject.Destroy(obj);
    }
    public static void Clear() {
      if (CombatHUDEquipmentPanel.Instance != null) {
        GameObject tmp = CombatHUDEquipmentPanel.Instance.gameObject;
        CombatHUDEquipmentPanel.Instance = null;
        CombatHUDEquipmentPanel.DestroyRec(tmp);
      }
    }
    public static void Init(CombatHUD HUD, CombatHUDWeaponPanel weaponPanel) {
      if (Instance != null) { CombatHUDEquipmentPanel.Clear(); };
      Transform labels = weaponPanel.gameObject.transform.Find("wp_Labels");
      if (labels != null) {
        GameObject labels_ex = GameObject.Instantiate(labels.gameObject);
        WeaponSlotsLabelsToggle toggle = labels_ex.gameObject.GetComponent<WeaponSlotsLabelsToggle>();
        if (toggle != null) { GameObject.Destroy(toggle); }
        Instance = labels_ex.AddComponent<CombatHUDEquipmentPanel>();
        Instance.HUD = HUD;
        Instance.weaponPanel = weaponPanel;
        Instance.slots = new List<CombatHUDEquipmentSlotEx>();
        labels_ex.transform.SetParent(weaponPanel.transform);
        labels_ex.transform.localScale = new Vector3(1f, 1f, 1f);
        labels_ex.SetActive(true);
        Instance.label_WeaponText = labels_ex.transform.Find("text_WeaponText").gameObject.GetComponent<LocalizableText>();
        Instance.label_State = labels_ex.transform.Find("text_Ammo").gameObject.GetComponent<LocalizableText>();
        Instance.label_Charges = labels_ex.transform.Find("text_Damage").gameObject.GetComponent<LocalizableText>();
        Instance.label_FailChance = labels_ex.transform.Find("text_HitChance").gameObject.GetComponent<LocalizableText>();
        Instance.timeInCurrentState = 0f;
        Instance.state = WPState.Off;
        Instance.label_WeaponText.SetText("COMPONENT");
        //GameObject.Destroy(text_Damage.gameObject);
        //Instance.label_State.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        //Instance.label_State.enableAutoSizing = false;
        //Instance.label_State.enableWordWrapping = false;
        Instance.label_State.SetText("STATE");
        Instance.label_Charges.SetText("CHARGES");
        //Instance.label_FailChance.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        //Instance.label_FailChance.enableAutoSizing = false;
        //Instance.label_FailChance.enableWordWrapping = false;
        Instance.label_FailChance.SetText("FAIL%");
      }
    }
    public void InitDisplayedEquipment(AbstractActor unit) {
      operatinalSlots.Clear();
      if (unit == null) { return; }
      Log.TWL(0, "CombatHUDEquipmentPanel.InitDisplayedEquipment unit:" + new Localize.Text(unit.DisplayName).ToString() + " pilot:" + unit.GetPilot().pilotDef.Description.Id);
      //Log.WL(1, "unit:" + new Localize.Text(unit.DisplayName).ToString());
      HashSet<MechComponent> acomps = new HashSet<MechComponent>();
      foreach (Ability ability in unit.ComponentAbilities) {
        acomps.Add(ability.parentComponent);
      }
      List<MechComponent> ncomps = new List<MechComponent>();
      foreach (MechComponent component in unit.allComponents) {
        Log.WL(1, "component:" + component.defId);
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (acomps.Contains(component)) { Log.WL(2, "have ability"); ncomps.Add(component); continue; };
        if (activatable == null) { Log.WL(2, "not activatable"); continue; }
        if (ActivatableComponent.isComponentActivated(component)) { Log.WL(2, "activated"); ncomps.Add(component); continue; };
        if (activatable.CanBeactivatedManualy) { Log.WL(2, "can be activated manualy"); ncomps.Add(component); continue; };
        if (activatable.AutoActivateOnHeat > Core.Epsilon) { Log.WL(2, "activate by heat"); ncomps.Add(component); continue; }
      }
      for (int index = 0; index < ncomps.Count; ++index) {
        if (index >= slots.Count) {
          CombatHUDEquipmentSlotEx nslot = CombatHUDEquipmentSlotEx.Init(HUD, weaponPanel, this);
          if (nslot != null) clickReceivers.Add(nslot.hoverSidePanel, nslot);
        }
        if (index < slots.Count) {
          operatinalSlots.Add(slots[index]);
          slots[index].Init(ncomps[index]);
          slots[index].RealState = true;
        }
      }
      for (int index = ncomps.Count; index < slots.Count; ++index) {
        slots[index].Hide();
      }
      CombatHUDEquipmentSlotEx.Clear();
      timeInCurrentState = 0f;
      state = WPState.Loading;
      RefreshDisplayedEquipment(unit);
    }
    public void RefreshDisplayedEquipment(AbstractActor unit) {
      //Log.TWL(0, "CombatHUDEquipmentPanel.RefreshDisplayedEquipment");
      if (unit == null) { return; }
      foreach (CombatHUDEquipmentSlotEx slot in slots) {
        slot.RefreshComponent();
      }
    }
    public void ShowWeaponsUpTo(int count) {
      if (count > this.operatinalSlots.Count) { count = this.operatinalSlots.Count; }
      for (int index = 0; index < count; ++index) {
        this.operatinalSlots[index].RealState = true;
        this.operatinalSlots[index].Show();
      }
      for (int index = count; index < this.operatinalSlots.Count; ++index) {
        this.operatinalSlots[index].Hide();
      }
    }
    private void SetState(WPState newState) {
      if (this.state == newState)
        return;
      this.state = newState;
      this.timeInCurrentState = 0.0f;
      switch (newState) {
        case WPState.Off:
          this.ShowWeaponsUpTo(0);
          break;
        case WPState.Loaded:
          this.ShowWeaponsUpTo(this.operatinalSlots.Count);
          break;
      }
    }
    private void Update() {
      this.timeInCurrentState += Time.unscaledDeltaTime;
      switch (this.state) {
        case WPState.Loading:
          if ((double)this.timeInCurrentState > (double)this.TimeToLoad) {
            this.SetState(WPState.Loaded);
            break;
          }
          break;
        case WPState.Unloading:
          if ((double)this.timeInCurrentState > (double)this.TimeToUnload) {
            this.SetState(WPState.Off);
            break;
          }
          break;
      }
      switch (this.state) {
        case WPState.Loading:
          this.ShowWeaponsUpTo((int)((double)this.operatinalSlots.Count * (double)(this.timeInCurrentState / this.TimeToLoad)));
          break;
        case WPState.Unloading:
          this.ShowWeaponsUpTo((int)((double)this.operatinalSlots.Count * (1.0 - (double)this.timeInCurrentState / (double)this.TimeToLoad)));
          break;
      }
    }
  }

  public class CombatHUDEquipmentSlotEx : EventTrigger {
    public CombatHUD HUD { get; private set; }
    public CombatHUDWeaponPanel weaponPanel { get; private set; }
    public CombatHUDEquipmentPanel equipPanel { get; private set; }
    public MechComponent component { get; private set; }
    public ActivatableComponent activeDef { get; private set; }
    public LocalizableText nameText { get; set; }
    public LocalizableText stateText { get; set; }
    public LocalizableText chargesText { get; set; }
    public LocalizableText failText { get; set; }
    public SVGImage mainImage { get; set; }
    public SVGImage checkImage { get; set; }
    public CombatHUDSidePanelHoverElement hoverSidePanel { get; set; }
    public List<CombatHUDEquipmentSlot> buttons { get; set; }
    public List<Ability> abilities { get; set; }
    public bool RealState { get; set; }
    private bool hovered { get; set; }
    private UILookAndColorConstants LookAndColorConstants { get; set; }
    private void ShowTextColor(Color color, Color failChanceColor) {
      this.nameText.color = color;
      this.stateText.color = color;
      this.failText.color = failChanceColor;
      this.chargesText.color = color;
    }
    private void RefreshHighlighted() {
      if ((this.component.IsFunctional == false) || (this.activeDef == null) || (HUD.SelectedTarget != null)) { return; }
      if (this.activeDef.CanBeactivatedManualy == false) { return; }
      if (ActivatableComponent.isOutOfCharges(component)) { return; }
      if (component.parent.HasMovedThisRound) { return; }
      if (component.parent.IsAvailableThisPhase == false) { return; }
      this.HUD.PlayAudioEvent(AudioEventList_ui.ui_weapon_hover);
      this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.HighlightedBGColor;
      this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.HighlightedTextColor, this.LookAndColorConstants.WeaponSlotColors.HighlightedTextColor);
    }
    private Color GetFailTextColor(float failChance) {
      if (failChance <= 0.15f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorA; }
      if (failChance <= 0.30f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorB; }
      if (failChance <= 0.50f) { return this.LookAndColorConstants.WeaponSlotColors.qualityColorC; }
      return this.LookAndColorConstants.WeaponSlotColors.qualityColorD;
    }
    private void RefreshNonHighlighted() {
      if (this.component.IsFunctional == false) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.DisabledBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor, this.LookAndColorConstants.WeaponSlotColors.DisabledTextColor);
        return;
      } else
      if ((activeDef == null) || (HUD.SelectedTarget != null) || ActivatableComponent.isOutOfCharges(component) || (component.parent.IsAvailableThisPhase == false) || (component.parent.HasMovedThisRound)) {
        this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.UnavailableSelBGColor;
        this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor, this.LookAndColorConstants.WeaponSlotColors.UnavailableSelTextColor);
        return;
      }
      this.mainImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      this.checkImage.color = this.LookAndColorConstants.WeaponSlotColors.AvailableBGColor;
      this.ShowTextColor(this.LookAndColorConstants.WeaponSlotColors.AvailableTextColor, this.GetFailTextColor(CombatHUDEquipmentSlotEx.FailChance(component)));
    }
    public override void OnPointerEnter(PointerEventData eventData) {
      Log.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerEnter " + component.defId);
      hovered = true;
      this.RefreshHighlighted();
    }
    public override void OnPointerExit(PointerEventData eventData) {
      Log.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerExit " + component.defId);
      hovered = false;
      this.RefreshNonHighlighted();
    }
    public override void OnPointerClick(PointerEventData eventData) {
      Log.TWL(0, "CombatHUDEquipmentSlotEx.OnPointerClick");
      if (activeDef == null) { return; }
      if (component == null) { return; }
      if (activeDef.CanBeactivatedManualy == false) { return; }
      if (HUD.SelectedTarget != null) { return; }
      if (component.IsFunctional == false) { return; }
      if (component.parent.HasMovedThisRound) { return; }
      if (ActivatableComponent.isOutOfCharges(component)) { return; }
      if (component.parent.IsAvailableThisPhase == false) { return; }
      Log.LogWrite("Toggle activatable " + component.defId + "\n");
      ActivatableComponent.toggleComponentActivation(this.component);
      equipPanel.RefreshDisplayedEquipment(component.parent);
    }
    public static Dictionary<MechComponent, string> stateCache = new Dictionary<MechComponent, string>();
    public static Dictionary<MechComponent, float> failCache = new Dictionary<MechComponent, float>();
    public static void Clear() { stateCache.Clear(); failCache.Clear(); }
    public static void ClearCache(MechComponent component) { stateCache.Remove(component); failCache.Remove(component); }
    public static float FailChance(MechComponent component) {
      if (failCache.TryGetValue(component, out float fail)) { return fail; }
      fail = ActivatableComponent.getEffectiveComponentFailChance(component);
      failCache.Add(component, fail);
      return fail;
    }
    public static string GetState(MechComponent component, ActivatableComponent activDef) {
      if (stateCache.TryGetValue(component, out string state)) { return state; }
      if (ActivatableComponent.isOutOfCharges(component)) {
        state = "__/CAE.OutOfCharges/__";
      } else {
        if (activDef.ChargesCount != 0) {
          state = "__/CAE.OPERATIONAL/__";
        } else {
          if (ActivatableComponent.isComponentActivated(component)) {
            state = activDef.ActivationMessage;
          } else {
            state = activDef.DeactivationMessage;
          }
        }
      }
      stateCache.Add(component, state);
      return state;
    }
    public void RefreshComponent() {
      this.Show();
      if (hovered == false) { RefreshNonHighlighted(); } else { RefreshHighlighted(); };
      nameText.SetText(component.UIName);
      if ((component.IsFunctional == false) || (activeDef == null)) {
        chargesText.SetText("--");
        stateText.SetText("--");
        failText.SetText("--");
      } else {
        if (activeDef.ChargesCount == -1) { chargesText.SetText("UNL"); } else
        if (activeDef.ChargesCount == 0) { chargesText.SetText("--"); } else {
          chargesText.SetText(ActivatableComponent.getChargesCount(component).ToString());
        }
        string state = CombatHUDEquipmentSlotEx.GetState(component, activeDef);
        stateText.SetText(state);
        failText.SetText("{0}%", CombatHUDEquipmentSlotEx.FailChance(component) * 100f);
      }
      AbstractActor actor = component.parent;
      bool forceInactive = actor.HasActivatedThisRound || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved;
      for (int index = 0; index < abilities.Count; ++index) {
        CombatHUDEquipmentSlotEx.ResetAbilityButton(actor, (CombatHUDActionButton)buttons[index], abilities[index], forceInactive);
      }
    }
    public void Show() {
      if (RealState) {
        //Log.TWL(0, "CombatHUDEquipmentSlotEx.Show "+component.defId+" abilities:"+abilities.Count);
        this.gameObject.SetActive(true);
        //this.gameObject.transform.parent.gameObject.SetActive(true);
        for (int index = 0; index < abilities.Count; ++index) {
          buttons[index].transform.parent.gameObject.SetActive(true);
          buttons[index].gameObject.SetActive(true);
        }
      }
    }
    public void Hide() {
      RealState = false;
      //this.abilities.Clear();
      this.gameObject.SetActive(false);
      //this.gameObject.transform.parent.gameObject.SetActive(false);
      foreach (CombatHUDEquipmentSlot button in buttons) {
        button.transform.parent.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
      }
    }
    public static void ResetAbilityButton(AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive) {
      if (ability == null)
        return;
      if (forceInactive)
        button.DisableButton();
      else if (button.IsAbilityActivated)
        button.ResetButtonIfNotActive(actor);
      else if (!ability.IsAvailable) {
        button.DisableButton();
      } else {
        bool flag1 = false;
        bool flag2 = false;
        bool flag3 = ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring;
        if (actor.HasActivatedThisRound || !actor.IsAvailableThisPhase || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved)
          button.DisableButton();
        else if (actor.IsShutDown) {
          if (!flag1)
            button.DisableButton();
          else
            button.ResetButtonIfNotActive(actor);
        } else if (actor.IsProne) {
          if (!flag2)
            button.DisableButton();
          else
            button.ResetButtonIfNotActive(actor);
        } else if ((actor.HasFiredThisRound || !actor.Combat.TurnDirector.IsInterleaved) && ability.Def.ActivationTime == AbilityDef.ActivationTiming.ConsumedByFiring)
          button.DisableButton();
        else if (actor.HasMovedThisRound) {
          if (flag3)
            button.ResetButtonIfNotActive(actor);
          else
            button.DisableButton();
        } else
          button.ResetButtonIfNotActive(actor);
      }
    }
    public void Init(MechComponent component) {
      hovered = false;
      this.component = component;
      this.activeDef = component.componentDef.GetComponent<ActivatableComponent>();
      this.hoverSidePanel.Title = new Localize.Text(component.Description.UIName);
      this.hoverSidePanel.Description = new Localize.Text(component.Description.Details);
      float offHeat = (activeDef.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activeDef.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activeDef.AutoDeactivateOnHeat;
      float onHeat = (activeDef.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? activeDef.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : activeDef.AutoActivateOnHeat;
      if (onHeat > Core.Epsilon) {
        this.hoverSidePanel.Description.Append("\nAUTO ACTIVE ON:" + onHeat);
      }
      if (offHeat > Core.Epsilon) {
        this.hoverSidePanel.Description.Append("\nAUTO DEACTIVE ON:" + offHeat);
      }
      int index = 0;
      AbstractActor actor = component.parent;
      abilities.Clear();
      CombatHUDEquipmentSlotEx.ClearCache(component);
      //HashSet<CombatHUDEquipmentSlot> enabledButtons = new HashSet<CombatHUDEquipmentSlot>();
      foreach (Ability ability in component.parent.ComponentAbilities) {
        if (ability.parentComponent == component) {
          if (buttons.Count <= index) {
            this.InitNewAbilitySlot();
          }
          if (buttons.Count > index) {
            buttons[index].gameObject.SetActive(true);
            buttons[index].gameObject.transform.parent.gameObject.SetActive(true);
            buttons[index].Init(component.parent.Combat, HUD, BTInput.Instance.Key_None(), true);
            buttons[index].InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false), ability, ability.Def.AbilityIcon, ability.Def.Description.Id, ability.Def.Description.Name, actor);
            bool forceInactive = actor.HasActivatedThisRound || actor.MovingToPosition != null || actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved;
            CombatHUDEquipmentSlotEx.ResetAbilityButton(actor, (CombatHUDActionButton)buttons[index], ability, forceInactive);
            ++index;
            abilities.Add(ability);
          }
        }
      }
      for (int t = abilities.Count; t < buttons.Count; ++t) { buttons[t].gameObject.SetActive(false); buttons[t].gameObject.transform.parent.gameObject.SetActive(false); };
      Log.TWL(0, "CombatHUDEquipmentSlotEx.Init " + component.defId + " abilities:" + this.abilities.Count);
    }
    public static CombatHUDEquipmentSlotEx Init(CombatHUD HUD, CombatHUDWeaponPanel weaponPanel, CombatHUDEquipmentPanel equipPanel) {
      Transform slot = weaponPanel.gameObject.transform.Find("wp_Slot1");
      GameObject slot_ex = null;
      if (slot != null) {
        slot_ex = GameObject.Instantiate(slot.gameObject);
        CombatHUDWeaponSlot hudslot = slot_ex.GetComponentInChildren<CombatHUDWeaponSlot>();
        GameObject.Destroy(hudslot);
        slot_ex.transform.SetParent(weaponPanel.transform);
        slot_ex.SetActive(false);
        slot_ex.transform.localScale = new Vector3(1f, 1f, 1f);
        Log.TWL(0, "found wp_Slot1 parent:" + slot_ex.transform.parent.name);
      } else {
        return null;
      }
      CombatHUDEquipmentSlotEx result = null;
      Transform ui = slot_ex.transform.Find("uixPrfBttn_weaponSlot-MANAGED");
      if (ui != null) {
        result = slot_ex.AddComponent<CombatHUDEquipmentSlotEx>();
        result.HUD = HUD;
        result.weaponPanel = weaponPanel;
        result.equipPanel = equipPanel;
        result.buttons = new List<CombatHUDEquipmentSlot>();
        result.abilities = new List<Ability>();
        result.LookAndColorConstants = LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants;
        result.nameText = ui.Find("weapon_Text").GetComponent<LocalizableText>();
        result.stateText = ui.Find("ammo_Text").GetComponent<LocalizableText>();
        result.failText = ui.Find("hitChance_Text").GetComponent<LocalizableText>();
        result.chargesText = ui.Find("damage_Text").GetComponent<LocalizableText>();
        result.mainImage = ui.gameObject.GetComponent<SVGImage>();
        result.checkImage = ui.Find("check_Image").gameObject.GetComponent<SVGImage>();
        Log.TWL(0, "check_Image:" + ui.Find("check_Image").gameObject.GetComponent<SVGImage>().color);
        GameObject.Destroy(ui.gameObject.GetComponent<CombatHUDTooltipHoverElement>());
        result.hoverSidePanel = ui.gameObject.AddComponent<CombatHUDSidePanelHoverElement>();
        result.hoverSidePanel.Init(HUD);
        result.hoverSidePanel.Title = new Localize.Text("COMPONENT");
        result.hoverSidePanel.Description = new Localize.Text("Description");
        //GameObject.Destroy(ui.Find("check_Image").gameObject);
        //GameObject.Destroy(slot_ex.transform.Find("flag_multiTarget_Diamond (1)").gameObject);
      }
      result.equipPanel.slots.Add(result);
      return result;
    }
    public void InitNewAbilitySlot() {
      Transform slot = weaponPanel.gameObject.transform.Find("uixPrfPanl_ElectronicWarfareToggles");
      if (slot != null) {
        GameObject slot_ex = GameObject.Instantiate(slot.gameObject);
        slot_ex.transform.SetParent(weaponPanel.transform);
        slot_ex.transform.localScale = new Vector3(1f, 0.9f, 1f);
        slot_ex.SetActive(false);
        HorizontalLayoutGroup layout = slot_ex.GetComponent<HorizontalLayoutGroup>();
        RectOffset tempPadding = new RectOffset(
                layout.padding.left,
                layout.padding.right,
                layout.padding.top,
                layout.padding.bottom);
        tempPadding.left += 50;
        layout.padding = tempPadding;
        Log.TWL(0, "found uixPrfPanl_ElectronicWarfareToggles parent:" + slot_ex.transform.parent.name);
        CombatHUDEquipmentSlot eqslot = slot_ex.transform.Find("equipmentButton_1").GetComponent<CombatHUDEquipmentSlot>();
        if (eqslot != null) {
          buttons.Add(eqslot);
          Log.WL(1, "found CombatHUDEquipmentSlot parent:" + eqslot.transform.parent.name);
        }
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
  public static class CombatHUDWeaponPanel_Init {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance, CombatGameState Combat, CombatHUD HUD) {
      CombatHUDWeaponPanelExHelper.HUD = HUD;
      CombatHUDWeaponPanelExHelper.panelEx = __instance.gameObject.GetComponent<CombatHUDWeaponPanelEx>();
      if (CombatHUDWeaponPanelExHelper.panelEx == null) {
        CombatHUDWeaponPanelExHelper.panelEx = __instance.gameObject.AddComponent<CombatHUDWeaponPanelEx>();
        CombatHUDWeaponPanelExHelper.panelEx.Init(__instance, HUD);
        Transform labels = __instance.gameObject.transform.Find("wp_Labels");
        if (labels != null) {
          WeaponSlotsLabelsToggle toggle = labels.gameObject.GetComponent<WeaponSlotsLabelsToggle>();
          if (toggle == null) { toggle = labels.gameObject.AddComponent<WeaponSlotsLabelsToggle>(); }
          LocalizableText text_WeaponText = labels.gameObject.transform.Find("text_WeaponText").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_Ammo = labels.gameObject.transform.Find("text_Ammo").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_Damage = labels.gameObject.transform.Find("text_Damage").gameObject.GetComponent<LocalizableText>();
          LocalizableText text_HitChance = labels.gameObject.transform.Find("text_HitChance").gameObject.GetComponent<LocalizableText>();
          toggle.Init(text_WeaponText, text_Ammo, text_Damage, text_HitChance);
        }
      }
      if (CombatHUDEquipmentPanel.Instance != null) { CombatHUDEquipmentPanel.Clear(); };
      CombatHUDEquipmentPanel.Init(HUD, __instance);
    }
  }

  [HarmonyPatch(typeof(CombatHUDWeaponPanel))]
  [HarmonyPatch("DisplayedActor")]
  [HarmonyPatch(MethodType.Setter)]
  public static class CombatHUDWeaponPanel_SetState {
    //public static bool Prepare() { return false; }
    public static void Postfix(CombatHUDWeaponPanel __instance) {
      if (CombatHUDWeaponPanelExHelper.panelEx != null) {
        CombatHUDWeaponPanelExHelper.panelEx.Force(__instance.DisplayedActor != null);
      }
    }
  }

  public static class CombatHUDWeaponPanelExHelper {
    public static CombatHUD HUD = null;
    public static CombatHUDWeaponPanelEx panelEx = null;
    public static void Clear() {
      HUD = null;
      if (panelEx != null) { GameObject.Destroy(panelEx); panelEx = null; }
    }
  }
  public enum WPState {
    None,
    Off,
    Loading,
    Loaded,
    Unloading
  }
  public class CombatHUDWeaponPanelEx : MonoBehaviour {
    private CombatHUDWeaponPanel panel;
    private CombatHUD HUD;
    private List<CombatHUDWeaponSlot> WeaponSlots;
    private List<CombatHUDEquipmentSlot> EquipmentSlots;
    private CombatHUDWeaponSlot meleeSlot;
    private CombatHUDWeaponSlot dfaSlot;
    public float TimeToLoad = 0.3f;
    public float TimeToUnload = 0.15f;
    private float timeInCurrentState;
    private WPState state;
    private PropertyInfo p_numWeaponsDisplayed;
    private int numWeaponsDisplayed {
      get {
        if (this.panel == null) { Log.TWL(0, "CombatHUDWeaponPanel is null!"); return 0; }
        return (int)p_numWeaponsDisplayed.GetValue(this.panel);
      }
    }
    public void Awake() {
      //panel = null;
    }
    public void Init(CombatHUDWeaponPanel weaponPanel, CombatHUD HUD) {
      this.panel = weaponPanel;
      this.HUD = HUD;
      WeaponSlots = (List<CombatHUDWeaponSlot>)typeof(CombatHUDWeaponPanel).GetField("WeaponSlots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      EquipmentSlots = (List<CombatHUDEquipmentSlot>)typeof(CombatHUDWeaponPanel).GetField("EquipmentSlots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      meleeSlot = (CombatHUDWeaponSlot)typeof(CombatHUDWeaponPanel).GetField("meleeSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      dfaSlot = (CombatHUDWeaponSlot)typeof(CombatHUDWeaponPanel).GetField("dfaSlot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weaponPanel);
      p_numWeaponsDisplayed = typeof(CombatHUDWeaponPanel).GetProperty("numWeaponsDisplayed", BindingFlags.Instance | BindingFlags.NonPublic);
      this.state = WPState.Off;
      this.timeInCurrentState = 0.0f;
    }
    public void Show(bool show) {
      if (show) { this.SetState(WPState.Loading); } else { this.SetState(WPState.Unloading); };
    }

    public void Toggle() {
      switch (this.state) {
        case WPState.Unloading:
        case WPState.Off:
          SetState(WPState.Loading);
          break;
        case WPState.Loaded:
        case WPState.Loading:
          SetState(WPState.Unloading);
          break;
      }
    }
    public void Force(bool show) {
      this.state = show ? WPState.Loaded : WPState.Off;
    }
    public void ShowWeaponsUpTo(int count) {
      typeof(CombatHUDWeaponPanel).GetMethod("ShowWeaponsUpTo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this.panel, new object[] { count });
    }
    private void SetState(WPState newState) {
      if (this.state == newState)
        return;
      this.state = newState;
      this.timeInCurrentState = 0.0f;
      switch (newState) {
        case WPState.Off:
          this.ShowWeaponsUpTo(0);
          break;
        case WPState.Loaded:
          this.ShowWeaponsUpTo(this.panel.DisplayedActor.Weapons.Count);
          break;
      }
    }
    private void Update() {
      this.timeInCurrentState += Time.unscaledDeltaTime;
      switch (this.state) {
        case WPState.Loading:
          if ((double)this.timeInCurrentState > (double)this.TimeToLoad) {
            this.SetState(WPState.Loaded);
            break;
          }
          break;
        case WPState.Unloading:
          if ((double)this.timeInCurrentState > (double)this.TimeToUnload) {
            this.SetState(WPState.Off);
            break;
          }
          break;
      }
      switch (this.state) {
        case WPState.Loading:
          this.ShowWeaponsUpTo((int)((double)this.panel.DisplayedActor.Weapons.Count * (double)(this.timeInCurrentState / this.TimeToLoad)));
          break;
        case WPState.Unloading:
          this.ShowWeaponsUpTo((int)((double)this.numWeaponsDisplayed * (1.0 - (double)this.timeInCurrentState / (double)this.TimeToLoad)));
          break;
      }
    }
  }
}
