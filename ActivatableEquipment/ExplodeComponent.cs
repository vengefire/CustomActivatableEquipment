﻿using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using CustomComponents;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Building = BattleTech.Building;
using Random = UnityEngine.Random;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("InitStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechComponent_InitStats {
    public static void Postfix(MechComponent __instance) {
      __instance.InitExplosionStats();
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("CancelCreatedEffects")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(bool) })]
  public static class MechComponent_CancelCreatedEffects {
    public static bool Prefix(MechComponent __instance,ref HashSet<EffectData> __state) {
      Log.LogWrite(__instance.parent.GUID+":"+__instance.defId+".CancelCreatedEffects prefix\n");
      __state = new HashSet<EffectData>();
      for (int index1 = 0; index1 < __instance.createdEffectIDs.Count; ++index1) {
        List<Effect> allEffectsWithId = __instance.parent.Combat.EffectManager.GetAllEffectsWithID(__instance.createdEffectIDs[index1]);
        for (int index2 = 0; index2 < allEffectsWithId.Count; ++index2) {
          EffectData statusEffect = allEffectsWithId[index2].EffectData;
          if (statusEffect.statisticData == null) { continue; };
          if (statusEffect.targetingData.effectTriggerType != EffectTriggerType.Passive) { continue; };
          if (statusEffect.targetingData.effectTargetType != EffectTargetType.Creator) { continue; };
          if ((allEffectsWithId[index2].EffectData.effectType == EffectType.StatisticEffect) && (allEffectsWithId[index2].EffectData.statisticData.effectsPersistAfterDestruction == true)) {
            if (__state.Contains(statusEffect) == false) {
              __state.Add(statusEffect);
              Log.LogWrite(" " + allEffectsWithId[index2].EffectData.Description.Id + " need to be restored\n");
            }
          }
        }
      }
      return true;
    }
    public static void Postfix(MechComponent __instance, ref HashSet<EffectData> __state) {
      Log.LogWrite(__instance.parent.GUID + ":" + __instance.defId + ".CancelCreatedEffects postfix\n");
      MechComponent component = __instance;
      foreach (EffectData statusEffect in __state) {
        string effectID = string.Format("ActivatableEffect_{0}_{1}", (object)component.parent.GUID, (object)component.uid);
        typeof(MechComponent).GetMethod("ApplyPassiveEffectToTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, new object[4] {
                (object)statusEffect,(object)component.parent,(object)((ICombatant)component.parent),(object)effectID
              });
        component.createdEffectIDs.Add(effectID);
        Log.LogWrite("restore effect " + effectID + ":" + statusEffect.Description.Id + "\n");
      }
    }
  }
  public class AoEComponentExplosionHitRecord {
    public Vector3 hitPosition;
    public float Damage;
    public AoEComponentExplosionHitRecord(Vector3 pos, float dmg) {
      this.hitPosition = pos;
      this.Damage = dmg;
    }
  }
  public class AoEComponentExplosionRecord {
    public ICombatant target;
    public float HeatDamage;
    public float StabDamage;
    public Dictionary<int, AoEComponentExplosionHitRecord> hitRecords;
    public AoEComponentExplosionRecord(ICombatant trg) {
      this.target = trg;
      this.HeatDamage = 0f;
      this.StabDamage = 0f;
      this.hitRecords = new Dictionary<int, AoEComponentExplosionHitRecord>();
    }
  }
  public class AoETempDesignMaskData {
    public MechComponent component;
    public Weapon fakeWeapon;
    public AoETempDesignMaskData(MechComponent c, Weapon fw) {
      component = c;
      fakeWeapon = fw;
    }
    public void procTempDesignMask() {
      if (this.component == null) { return; };
      if (this.fakeWeapon == null) { return; };
      this.component.applyImpactTempMask();
    }
  }
  public static class AoEComponentExplodeHelper {
    public static Dictionary<int, float> MechHitLocations = null;
    public static Dictionary<int, float> VehicleLocations = null;
    public static Dictionary<int, float> OtherLocations = null;
    public static readonly float AOEHitIndicator = -10f;
    public static void InitActorStat(this MechComponent component,float value,string name) {
      if (value >= 0f) {
        if (string.IsNullOrEmpty(name) == false) {
          if (Core.checkExistance(component.parent.StatCollection, name)) {
            component.parent.StatCollection.Set<float>(name, value);
          } else {
            component.parent.StatCollection.AddStatistic<float>(name, value);
          }
        }
      }
    }
    /*public static void InitActorStat(this MechComponent component, int value, string name) {
      if (value >= 0) {
        if (string.IsNullOrEmpty(name) == false) {
          if (Core.checkExistance(component.parent.StatCollection, name)) {
            component.parent.StatCollection.Set<int>(name, value);
          } else {
            component.parent.StatCollection.AddStatistic<int>(name, value);
          }
        }
      }
    }*/
    public static void InitActorStat(this MechComponent component, string value, string name) {
      if (value != null) {
        if (string.IsNullOrEmpty(name) == false) {
          if (Core.checkExistance(component.parent.StatCollection, name)) {
            component.parent.StatCollection.Set<string>(name, value);
          } else {
            component.parent.StatCollection.AddStatistic<string>(name, value);
          }
        }
      }
    }
    public static void InitExplosionStats(this MechComponent component) {
      Log.LogWrite("MechComponent.InitExplosionStats(" + component.parent.GUID+":"+component.defId+")\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        Log.LogWrite(" not activatable\n");
        return;
      }
      component.InitActorStat(activatable.Explosion.Range, activatable.Explosion.RangeActorStat);
      component.InitActorStat(activatable.Explosion.Damage, activatable.Explosion.DamageActorStat);
      component.InitActorStat(activatable.Explosion.Heat, activatable.Explosion.HeatActorStat);
      component.InitActorStat(activatable.Explosion.Stability, activatable.Explosion.StabilityActorStat);
      component.InitActorStat(activatable.Explosion.Chance, activatable.Explosion.ChanceActorStat);
      component.InitActorStat(activatable.Explosion.VFX, activatable.Explosion.VFXActorStat);
      component.InitActorStat(activatable.Explosion.VFXOffsetX, activatable.Explosion.VFXOffsetXActorStat);
      component.InitActorStat(activatable.Explosion.VFXOffsetY, activatable.Explosion.VFXOffsetYActorStat);
      component.InitActorStat(activatable.Explosion.VFXOffsetZ, activatable.Explosion.VFXOffsetZActorStat);
      component.InitActorStat(activatable.Explosion.VFXScaleX, activatable.Explosion.VFXScaleXActorStat);
      component.InitActorStat(activatable.Explosion.VFXScaleY, activatable.Explosion.VFXScaleYActorStat);
      component.InitActorStat(activatable.Explosion.VFXScaleZ, activatable.Explosion.VFXScaleZActorStat);
      component.InitActorStat(activatable.Explosion.FireTerrainChance, activatable.Explosion.FireTerrainChanceActorStat);
      component.InitActorStat(activatable.Explosion.FireTerrainStrength, activatable.Explosion.FireTerrainStrengthActorStat);
      component.InitActorStat(activatable.Explosion.FireDurationWithoutForest, activatable.Explosion.FireDurationWithoutForestActorStat);
      component.InitActorStat(activatable.Explosion.FireTerrainCellRadius, activatable.Explosion.FireTerrainCellRadiusActorStat);
      component.InitActorStat(activatable.Explosion.TempDesignMask, activatable.Explosion.TempDesignMaskActorStat);
      component.InitActorStat(activatable.Explosion.TempDesignMaskTurns, activatable.Explosion.TempDesignMaskTurnsActorStat);
      component.InitActorStat(activatable.Explosion.TempDesignMaskCellRadius, activatable.Explosion.TempDesignMaskCellRadiusActorStat);
      component.InitActorStat(activatable.Explosion.LongVFX, activatable.Explosion.LongVFXActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXScaleX, activatable.Explosion.LongVFXScaleXActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXScaleY, activatable.Explosion.LongVFXScaleYActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXScaleZ, activatable.Explosion.LongVFXScaleZActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXOffsetX, activatable.Explosion.LongVFXOffsetXActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXOffsetY, activatable.Explosion.LongVFXOffsetYActorStat);
      component.InitActorStat(activatable.Explosion.LongVFXOffsetZ, activatable.Explosion.LongVFXOffsetZActorStat);
      component.InitActorStat(activatable.Explosion.ExplodeSound, activatable.Explosion.ExplodeSoundActorStat);
      component.InitActorStat(activatable.Explosion.statusEffectsCollection, activatable.Explosion.statusEffectsCollectionActorStat);
      Log.LogWrite(" StatusEffectsCollections "+ activatable.Explosion.statusEffects.Length + "\n");
      if (activatable.Explosion.statusEffects.Length > 0) {
        Log.LogWrite(" found StatusEffectsCollections\n");
        if (AoEExplosion.ExposionStatusEffects.ContainsKey(component.parent) == false) { AoEExplosion.ExposionStatusEffects.Add(component.parent, new Dictionary<string, List<EffectData>>()); };
        Log.LogWrite(" parent:"+component.parent.DisplayName+":"+component.parent.GUID+"\n");
        Dictionary<string, List<EffectData>> statusEffectsCollection = AoEExplosion.ExposionStatusEffects[component.parent];
        if (statusEffectsCollection.ContainsKey(activatable.Explosion.statusEffectsCollectionName) == false) { statusEffectsCollection.Add(activatable.Explosion.statusEffectsCollectionName, new List<EffectData>()); }
        statusEffectsCollection[activatable.Explosion.statusEffectsCollectionName].AddRange(activatable.Explosion.statusEffects);
      }
    }
    public static List<EffectData> AoEExplosionEffects(this MechComponent component) {
      Log.LogWrite(component.parent.GUID + ":" + component.defId + ".AoEExplosionEffects\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return new List<EffectData>();
      }
      if (AoEExplosion.ExposionStatusEffects.ContainsKey(component.parent) == false) {
        Log.LogWrite(" parent have no explosion effects collections\n");
        return new List<EffectData>();
      }
      Dictionary<string, List<EffectData>> statusEffectsCollection = AoEExplosion.ExposionStatusEffects[component.parent];
      string collectionName = string.Empty;
      if (string.IsNullOrEmpty(activatable.Explosion.statusEffectsCollectionActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.statusEffectsCollectionActorStat)) {
          Log.LogWrite(activatable.Explosion.statusEffectsCollectionActorStat + " exists\n");
          collectionName = component.parent.StatCollection.GetStatistic(activatable.Explosion.statusEffectsCollectionActorStat).Value<string>();
        } else {
          Log.LogWrite(activatable.Explosion.statusEffectsCollectionActorStat + " not exists\n");
          collectionName = activatable.Explosion.statusEffectsCollection;
        }
      } else {
        Log.LogWrite("statusEffectsCollectionActorStat not set\n");
        collectionName = activatable.Explosion.statusEffectsCollection;
      }
      if(statusEffectsCollection.ContainsKey(collectionName) == false) {
        Log.LogWrite(" actor explosion effects collection not contains collection "+collectionName+"\n");
        return new List<EffectData>();
      }
      return statusEffectsCollection[collectionName];
    }
    public static float AoEExplodeRange(this MechComponent component) {
      Log.LogWrite(component.parent.GUID + ":" + component.defId + ".AoEExplodeRange\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.RangeActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.RangeActorStat)) {
          Log.LogWrite(activatable.Explosion.RangeActorStat + " exists\n");
          return component.parent.StatCollection.GetStatistic(activatable.Explosion.RangeActorStat).Value<float>();
        } else {
          Log.LogWrite(activatable.Explosion.RangeActorStat + " not exists\n");
          return activatable.Explosion.Range;
        }
      }
      Log.LogWrite("RangeActorStat not set\n");
      return activatable.Explosion.Range;
    }
    public static float AoEExplodeDamage(this MechComponent component) {
      Log.LogWrite(component.parent.GUID + ":" + component.defId + ".AoEExplodeDamage\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      float result = 0f;
      if (string.IsNullOrEmpty(activatable.Explosion.DamageActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.DamageActorStat)) {
          Log.LogWrite(activatable.Explosion.DamageActorStat + " exists\n");
          result = component.parent.StatCollection.GetStatistic(activatable.Explosion.DamageActorStat).Value<float>();
        } else {
          Log.LogWrite(activatable.Explosion.DamageActorStat + " not exists\n");
          result = activatable.Explosion.Damage;
        }
      } else {
        Log.LogWrite("DamageActorStat not set\n");
        result = activatable.Explosion.Damage;
      }
      if (activatable.Explosion.AmmoCountScale) {
        AmmunitionBox box = component as AmmunitionBox;
        if(box != null) {
          if (box.IsFunctional == false) {
            Log.LogWrite(" ammo box not functional. Exploded already.");
            result = 0f;
          } else {
            Log.LogWrite("Scale by ammo capacity. Was "+result);
            result *= ((float)box.CurrentAmmo / (float)box.AmmoCapacity);
            Log.LogWrite(" become " + result+"\n");
          }
        }
      }
      if(string.IsNullOrEmpty(activatable.Explosion.AddSelfDamageTag) == false) {
        Log.LogWrite(" alter by tag\n");
        foreach (MechComponent scomp in component.parent.allComponents) {
          ActivatableComponent sactiv = scomp.componentDef.GetComponent<ActivatableComponent>();
          if (sactiv == null) { continue; }
          if (component.IsFunctional == false) { continue; }
          if(sactiv.Explosion.AddOtherDamageTag == activatable.Explosion.AddSelfDamageTag) {
            result += scomp.AoEExplodeDamage();
          }
        }
      }
      return result;
    }
    public static float AoEExplodeHeat(this MechComponent component) {
      Log.LogWrite(component.parent.GUID + ":" + component.defId + ".AoEExplodeHeat\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      float result = 0f;
      if (string.IsNullOrEmpty(activatable.Explosion.HeatActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.HeatActorStat)) {
          Log.LogWrite(activatable.Explosion.HeatActorStat + " exists\n");
          result = component.parent.StatCollection.GetStatistic(activatable.Explosion.HeatActorStat).Value<float>();
        } else {
          Log.LogWrite(activatable.Explosion.HeatActorStat + " not exists\n");
          result = activatable.Explosion.Heat;
        }
      } else {
        Log.LogWrite("HeatActorStat not set\n");
        result = activatable.Explosion.Heat;
      }
      if (activatable.Explosion.AmmoCountScale) {
        AmmunitionBox box = component as AmmunitionBox;
        if (box != null) {
          if (box.IsFunctional == false) {
            Log.LogWrite(" ammo box not functional. Exploded already.");
            result = 0f;
          } else {
            Log.LogWrite("Scale by ammo capacity. Was " + result);
            result *= ((float)box.CurrentAmmo / (float)box.AmmoCapacity);
            Log.LogWrite(" become " + result + "\n");
          }
        }
      }
      if (string.IsNullOrEmpty(activatable.Explosion.AddSelfDamageTag) == false) {
        Log.LogWrite(" alter by tag\n");
        foreach (MechComponent scomp in component.parent.allComponents) {
          ActivatableComponent sactiv = scomp.componentDef.GetComponent<ActivatableComponent>();
          if (sactiv == null) { continue; }
          if (component.IsFunctional == false) { continue; }
          if (sactiv.Explosion.AddOtherDamageTag == activatable.Explosion.AddSelfDamageTag) {
            result += scomp.AoEExplodeHeat();
          }
        }
      }
      return result;
    }
    public static float AoEExplodeStability(this MechComponent component) {
      Log.LogWrite(component.parent.GUID + ":" + component.defId + ".AoEExplodeStability\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      float result = 0f;
      if (string.IsNullOrEmpty(activatable.Explosion.StabilityActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.StabilityActorStat)) {
          Log.LogWrite(activatable.Explosion.StabilityActorStat + " exists\n");
          result = component.parent.StatCollection.GetStatistic(activatable.Explosion.StabilityActorStat).Value<float>();
        } else {
          Log.LogWrite(activatable.Explosion.StabilityActorStat + " not exists\n");
          result = activatable.Explosion.Stability;
        }
      } else {
        Log.LogWrite("StabilityActorStat not set\n");
        result = activatable.Explosion.Stability;
      }
      if (activatable.Explosion.AmmoCountScale) {
        AmmunitionBox box = component as AmmunitionBox;
        if (box != null) {
          if (box.IsFunctional == false) {
            Log.LogWrite(" ammo box not functional. Exploded already.");
            result = 0f;
          } else {
            Log.LogWrite("Scale by ammo capacity. Was " + result);
            result *= ((float)box.CurrentAmmo / (float)box.AmmoCapacity);
            Log.LogWrite(" become " + result + "\n");
          }
        }
      }
      if (string.IsNullOrEmpty(activatable.Explosion.AddSelfDamageTag) == false) {
        Log.LogWrite(" alter by tag\n");
        foreach (MechComponent scomp in component.parent.allComponents) {
          ActivatableComponent sactiv = scomp.componentDef.GetComponent<ActivatableComponent>();
          if (sactiv == null) { continue; }
          if (component.IsFunctional == false) { continue; }
          if (sactiv.Explosion.AddOtherDamageTag == activatable.Explosion.AddSelfDamageTag) {
            result += scomp.AoEExplodeStability();
          }
        }
      }
      return result;
    }
    public static float AoEExplodeChance(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.ChanceActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.ChanceActorStat)) {
          return component.parent.StatCollection.GetStatistic(activatable.Explosion.ChanceActorStat).Value<float>();
        } else {
          return activatable.Explosion.Chance;
        }
      }
      return activatable.Explosion.Chance;
    }
    public static string AoEExplodeVFX(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return string.Empty;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.VFXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXActorStat)) {
          return component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXActorStat).Value<string>();
        } else {
          return activatable.Explosion.VFX;
        }
      }
      return activatable.Explosion.VFX;
    }
    public static Vector3 AoEVFXScale(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return Vector3.one;
      }
      Vector3 result = Vector3.one;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXScaleXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXScaleXActorStat)) {
          result.x = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXScaleXActorStat).Value<float>();
        } else {
          result.x = activatable.Explosion.VFXScaleX;
        }
      }
      result.x = activatable.Explosion.VFXScaleX;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXScaleYActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXScaleYActorStat)) {
          result.y = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXScaleYActorStat).Value<float>();
        } else {
          result.y = activatable.Explosion.VFXScaleY;
        }
      }
      result.y = activatable.Explosion.VFXScaleY;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXScaleZActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXScaleZActorStat)) {
          result.z = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXScaleZActorStat).Value<float>();
        } else {
          result.z = activatable.Explosion.VFXScaleZ;
        }
      }
      result.z = activatable.Explosion.VFXScaleZ;
      return result;
    }
    public static Vector3 AoEVFXOffset(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return Vector3.one;
      }
      Vector3 result = Vector3.one;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXOffsetXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXOffsetXActorStat)) {
          result.x = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXOffsetXActorStat).Value<float>();
        } else {
          result.x = activatable.Explosion.VFXOffsetX;
        }
      }
      result.x = activatable.Explosion.VFXOffsetX;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXOffsetYActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXOffsetYActorStat)) {
          result.y = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXOffsetYActorStat).Value<float>();
        } else {
          result.y = activatable.Explosion.VFXOffsetY;
        }
      }
      result.y = activatable.Explosion.VFXOffsetY;
      if (string.IsNullOrEmpty(activatable.Explosion.VFXOffsetZActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.VFXOffsetZActorStat)) {
          result.z = component.parent.StatCollection.GetStatistic(activatable.Explosion.VFXOffsetZActorStat).Value<float>();
        } else {
          result.z = activatable.Explosion.VFXOffsetZ;
        }
      }
      result.z = activatable.Explosion.VFXOffsetZ;
      return result;
    }
    public static float AoEExplodeFireTerrainChance(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0f;
      }
      float result = 0f;
      if (string.IsNullOrEmpty(activatable.Explosion.FireTerrainChanceActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.FireTerrainChanceActorStat)) {
          result = component.parent.StatCollection.GetStatistic(activatable.Explosion.FireTerrainChanceActorStat).Value<float>();
        } else {
          result = activatable.Explosion.FireTerrainChance;
        }
      } else {
        result = activatable.Explosion.FireTerrainChance;
      }
      result *= DynamicMapHelper.BiomeLitFireChance();
      return result;
    }
    public static int AoEExplodeFireTerrainStrength(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0;
      }
      int result = 0;
      if (string.IsNullOrEmpty(activatable.Explosion.FireTerrainStrengthActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.FireTerrainStrengthActorStat)) {
          result = Mathf.RoundToInt(component.parent.StatCollection.GetStatistic(activatable.Explosion.FireTerrainStrengthActorStat).Value<float>());
        } else {
          result = Mathf.RoundToInt(activatable.Explosion.FireTerrainStrength);
        }
      } else {
        result = Mathf.RoundToInt(activatable.Explosion.FireTerrainStrength);
      }
      result = Mathf.RoundToInt((float)result * DynamicMapHelper.BiomeWeaponFireStrength());
      return result;
    }
    public static int AoEExplodeFireDurationWithoutForest(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0;
      }
      int result = 0;
      if (string.IsNullOrEmpty(activatable.Explosion.FireDurationWithoutForestActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.FireDurationWithoutForestActorStat)) {
          result = Mathf.RoundToInt(component.parent.StatCollection.GetStatistic(activatable.Explosion.FireDurationWithoutForestActorStat).Value<float>());
        } else {
          result = Mathf.RoundToInt(activatable.Explosion.FireDurationWithoutForest);
        }
      } else {
        result = Mathf.RoundToInt(activatable.Explosion.FireDurationWithoutForest);
      }
      result = Mathf.RoundToInt((float)result * DynamicMapHelper.BiomeWeaponFireDuration());
      return result;
    }
    public static int AoEExplodeFireFireTerrainCellRadius(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.FireTerrainCellRadiusActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.FireTerrainCellRadiusActorStat)) {
          return Mathf.RoundToInt(component.parent.StatCollection.GetStatistic(activatable.Explosion.FireTerrainCellRadiusActorStat).Value<float>());
        } else {
          return Mathf.RoundToInt(activatable.Explosion.FireTerrainCellRadius);
        }
      }
      return Mathf.RoundToInt(activatable.Explosion.FireTerrainCellRadius);
    }
    public static DesignMaskDef AoEExplodeTempDesignMask(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return null;
      }
      string maskName = null;
      if (string.IsNullOrEmpty(activatable.Explosion.TempDesignMaskActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.TempDesignMaskActorStat)) {
          maskName = component.parent.StatCollection.GetStatistic(activatable.Explosion.TempDesignMaskActorStat).Value<string>();
        } else {
          maskName = activatable.Explosion.TempDesignMask;
        }
      }
      maskName = activatable.Explosion.TempDesignMask;
      if (string.IsNullOrEmpty(maskName)) { return null; };
      if (DynamicMapHelper.loadedMasksDef.ContainsKey(maskName) == false) { return null; }
      return DynamicMapHelper.loadedMasksDef[maskName];
    }
    public static int AoEExplodeTempDesignMaskTurns(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.TempDesignMaskTurnsActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.TempDesignMaskTurnsActorStat)) {
          return Mathf.RoundToInt(component.parent.StatCollection.GetStatistic(activatable.Explosion.TempDesignMaskTurnsActorStat).Value<float>());
        } else {
          return Mathf.RoundToInt(activatable.Explosion.TempDesignMaskTurns);
        }
      }
      return Mathf.RoundToInt(activatable.Explosion.TempDesignMaskTurns);
    }
    public static int AoEExplodeTempDesignMaskCellRadius(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return 0;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.TempDesignMaskCellRadiusActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.TempDesignMaskCellRadiusActorStat)) {
          return Mathf.RoundToInt(component.parent.StatCollection.GetStatistic(activatable.Explosion.TempDesignMaskCellRadiusActorStat).Value<float>());
        } else {
          return Mathf.RoundToInt(activatable.Explosion.TempDesignMaskCellRadius);
        }
      }
      return Mathf.RoundToInt(activatable.Explosion.TempDesignMaskCellRadius);
    }
    public static string AoEExplodeLongVFX(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return string.Empty;
      }
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXActorStat)) {
          return component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXActorStat).Value<string>();
        } else {
          return activatable.Explosion.LongVFX;
        }
      }
      return activatable.Explosion.LongVFX;
    }
    public static Vector3 AoELongVFXScale(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return Vector3.one;
      }
      Vector3 result = Vector3.one;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXScaleXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXScaleXActorStat)) {
          result.x = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXScaleXActorStat).Value<float>();
        } else {
          result.x = activatable.Explosion.LongVFXScaleX;
        }
      }
      result.x = activatable.Explosion.LongVFXScaleX;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXScaleYActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXScaleYActorStat)) {
          result.y = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXScaleYActorStat).Value<float>();
        } else {
          result.y = activatable.Explosion.LongVFXScaleY;
        }
      }
      result.y = activatable.Explosion.LongVFXScaleY;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXScaleZActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXScaleZActorStat)) {
          result.z = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXScaleZActorStat).Value<float>();
        } else {
          result.z = activatable.Explosion.LongVFXScaleZ;
        }
      }
      result.z = activatable.Explosion.LongVFXScaleZ;
      return result;
    }
    public static Vector3 AoELongVFXOffset(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return Vector3.one;
      }
      Vector3 result = Vector3.one;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXOffsetXActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXOffsetXActorStat)) {
          result.x = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXOffsetXActorStat).Value<float>();
        } else {
          result.x = activatable.Explosion.LongVFXOffsetX;
        }
      }
      result.x = activatable.Explosion.LongVFXOffsetX;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXOffsetYActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXOffsetYActorStat)) {
          result.y = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXOffsetYActorStat).Value<float>();
        } else {
          result.y = activatable.Explosion.LongVFXOffsetY;
        }
      }
      result.y = activatable.Explosion.LongVFXOffsetY;
      if (string.IsNullOrEmpty(activatable.Explosion.LongVFXOffsetZActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.LongVFXOffsetZActorStat)) {
          result.z = component.parent.StatCollection.GetStatistic(activatable.Explosion.LongVFXOffsetZActorStat).Value<float>();
        } else {
          result.z = activatable.Explosion.LongVFXOffsetZ;
        }
      }
      result.z = activatable.Explosion.LongVFXOffsetZ;
      return result;
    }

    public static CustAmmoCategories.CustomAudioSource AoEExplodeSound(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        return null;
      }
      string soundStr = string.Empty;
      if (string.IsNullOrEmpty(activatable.Explosion.ExplodeSoundActorStat) == false) {
        if (Core.checkExistance(component.parent.StatCollection, activatable.Explosion.ExplodeSoundActorStat)) {
          soundStr = component.parent.StatCollection.GetStatistic(activatable.Explosion.ExplodeSoundActorStat).Value<string>();
        } else {
          soundStr = activatable.Explosion.ExplodeSound;
        }
      } else {
        soundStr = activatable.Explosion.ExplodeSound;
      }
      if (string.IsNullOrEmpty(soundStr)) { return null; }
      return new CustAmmoCategories.CustomAudioSource(soundStr);
    }

    public static void AoEPlayExplodeVFX(this MechComponent component) {
      VFXObjects vfxs = component.VFXObjects();
      if (vfxs == null) { return; };
      string vfxPrefab = component.AoEExplodeVFX();
      if (string.IsNullOrEmpty(vfxPrefab)) { return; };
      if (vfxs.explodeObject != null) { vfxs.explodeObject.CleanupSelf(); };
      vfxs.explodeObject = new ObjectSpawnDataSelf(vfxPrefab, component.parent.GameRep.gameObject,
                    component.AoELongVFXOffset(),
                    component.AoELongVFXScale(), true, false);
      vfxs.explodeObject.SpawnSelf(component.parent.Combat);
    }
    public static void applyExplodeBurn(this MechComponent component, Weapon fakeWeapon) {
      Log.LogWrite("Applying burn effect:" + component.defId + " " + component.parent.CurrentPosition + "\n");
      MapTerrainDataCellEx cell = component.parent.Combat.MapMetaData.GetCellAt(component.parent.CurrentPosition) as MapTerrainDataCellEx;
      if (cell == null) {
        CustomAmmoCategoriesLog.Log.LogWrite(" cell is not extended\n");
        return;
      }
      Log.LogWrite(" fire at " + component.parent.CurrentPosition + "\n");
      if (component.AoEExplodeFireFireTerrainCellRadius() == 0) {
        if (cell.hexCell.TryBurnCell(fakeWeapon,component.AoEExplodeFireTerrainChance(),component.AoEExplodeFireTerrainStrength(),component.AoEExplodeFireDurationWithoutForest())) {
          DynamicMapHelper.burningHexes.Add(cell.hexCell);
        };
      } else {
        List<MapTerrainHexCell> affectedHexCells = MapTerrainHexCell.listHexCellsByCellRadius(cell, component.AoEExplodeFireFireTerrainCellRadius());
        foreach (MapTerrainHexCell hexCell in affectedHexCells) {
          if (hexCell.TryBurnCell(fakeWeapon, component.AoEExplodeFireTerrainChance(), component.AoEExplodeFireTerrainStrength(), component.AoEExplodeFireDurationWithoutForest())) {
            DynamicMapHelper.burningHexes.Add(hexCell);
          };
        }
      }
    }
    public static void applyImpactTempMask(this MechComponent component) {
      Log.LogWrite("Applying long effect:" + component.defId + " " + component.parent.CurrentPosition + "\n");
      MapTerrainDataCellEx cell = component.parent.Combat.MapMetaData.GetCellAt(component.parent.CurrentPosition) as MapTerrainDataCellEx;
      if (cell == null) {
        Log.LogWrite(" cell is not extended\n");
        return;
      }
      Log.LogWrite(" impact at " + component.parent.CurrentPosition + "\n");
      int turns = component.AoEExplodeTempDesignMaskTurns();
      string vfx = component.AoEExplodeLongVFX();
      Vector3 scale = component.AoELongVFXScale();
      int radius = component.AoEExplodeTempDesignMaskCellRadius();
      DesignMaskDef mask = component.AoEExplodeTempDesignMask();
      if (radius == 0) {
        cell.hexCell.addTempTerrainVFX(component.parent.Combat, vfx, turns, scale);
        if (mask != null) DynamicMapHelper.addDesignMaskAsync(cell.hexCell, mask, turns);
      } else {
        List<MapTerrainHexCell> affectedHexCells = MapTerrainHexCell.listHexCellsByCellRadius(cell, radius);
        foreach (MapTerrainHexCell hexCell in affectedHexCells) {
          hexCell.addTempTerrainVFX(component.parent.Combat, vfx, turns, scale);
          if (mask != null) DynamicMapHelper.addDesignMaskAsync(hexCell, mask, turns);
        }
      }
    }
    public static Vector3 GetBuildingHitPosition(this LineOfSight LOS, AbstractActor attacker, BattleTech.Building target, Vector3 attackPosition, float weaponRange, Vector3 origHitPosition) {
      Vector3 a = origHitPosition;
      Vector3 vector3_1 = attackPosition + attacker.HighestLOSPosition;
      string guid = target.GUID;
      Vector3 collisionWorldPos = Vector3.zero;
      bool flag = false;
      if ((UnityEngine.Object)target.BuildingRep == (UnityEngine.Object)null)
        return a;
      foreach (Collider allRaycastCollider in target.GameRep.AllRaycastColliders) {
        if (LOS.HasLineOfFire(vector3_1, allRaycastCollider.bounds.center, guid, weaponRange, out collisionWorldPos)) {
          a = allRaycastCollider.bounds.center;
          flag = true;
          break;
        }
      }
      for (int index1 = 0; index1 < target.LOSTargetPositions.Length; ++index1) {
        if (LOS.HasLineOfFire(vector3_1, target.LOSTargetPositions[index1], guid, weaponRange, out collisionWorldPos)) {
          if (flag) {
            Vector3 end = Vector3.Lerp(a, target.LOSTargetPositions[index1], UnityEngine.Random.Range(0.0f, 0.15f));
            if (LOS.HasLineOfFire(vector3_1, end, guid, weaponRange, out collisionWorldPos))
              a = end;
          } else {
            Vector3 vector3_2 = a;
            for (int index2 = 0; index2 < 10; ++index2) {
              vector3_2 = Vector3.Lerp(vector3_2, target.LOSTargetPositions[index1], UnityEngine.Random.Range(0.1f, 0.6f));
              if (LOS.HasLineOfFire(vector3_1, vector3_2, guid, weaponRange, out collisionWorldPos)) {
                a = vector3_2;
                flag = true;
                break;
              }
            }
            if (!flag) {
              a = target.LOSTargetPositions[index1];
              flag = true;
            }
          }
        }
      }
      Ray ray = new Ray(vector3_1, a - vector3_1);
      foreach (Collider allRaycastCollider in target.GameRep.AllRaycastColliders) {
        GameObject gameObject = allRaycastCollider.gameObject;
        bool activeSelf = gameObject.activeSelf;
        gameObject.SetActive(true);
        RaycastHit hitInfo;
        if (allRaycastCollider.Raycast(ray, out hitInfo, 1000f)) {
          gameObject.SetActive(activeSelf);
          return hitInfo.point;
        }
        gameObject.SetActive(activeSelf);
      }
      return a;
    }
    public static Vector3 getImpactPositionSimple(this ICombatant initialTarget, AbstractActor attacker, Vector3 attackPosition, int hitLocation) {
      Vector3 impactPoint = initialTarget.CurrentPosition;
      AttackDirection attackDirection = AttackDirection.FromFront;
      if ((UnityEngine.Object)initialTarget.GameRep != (UnityEngine.Object)null) {
        impactPoint = initialTarget.GameRep.GetHitPosition(hitLocation);
        attackDirection = initialTarget.Combat.HitLocation.GetAttackDirection(attackPosition, initialTarget);
        if (initialTarget.UnitType == UnitType.Building) {
          impactPoint = attacker.Combat.LOS.GetBuildingHitPosition(attacker, initialTarget as BattleTech.Building, attackPosition, 100f, impactPoint);
        } else {
          Vector3 origin = attackPosition + attacker.HighestLOSPosition;
          Vector3 vector3_2 = impactPoint - origin;
          Ray ray2 = new Ray(origin, vector3_2.normalized);
          foreach (Collider allRaycastCollider in initialTarget.GameRep.AllRaycastColliders) {
            RaycastHit hitInfo;
            if (allRaycastCollider.Raycast(ray2, out hitInfo, vector3_2.magnitude)) {
              impactPoint = hitInfo.point;
              break;
            }
          }
        }
      }
      return impactPoint;
    }
    public static void InitHitLocationsAOE() {
      AoEComponentExplodeHelper.MechHitLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.CenterTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.CenterTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftArm] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightArm] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftLeg] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightLeg] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.Head] = 0f;
      AoEComponentExplodeHelper.VehicleLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Front] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Rear] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Left] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Right] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Turret] = 80f;
      AoEComponentExplodeHelper.OtherLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.OtherLocations[1] = 100f;
    }
    public static void AoEExplodeComponent(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (component.parent == null) { return; }
      float Range = component.AoEExplodeRange();
      float AoEDmg = component.AoEExplodeDamage();
      float Chance = component.AoEExplodeChance();
      if (AoEDmg <= Core.Epsilon) { return; }
      if (Range <= Core.Epsilon) { return; }
      if (Chance <= Core.Epsilon) { return; }
      float roll = Random.Range(0f, 1f);
      Log.LogWrite("AoE explosion "+component.parent.GUID+":"+component.defId+". Chance:"+Chance+" Roll:"+roll+"\n");
      if (roll > Chance) {
        Log.LogWrite("Fail. No Explosion\n");
        return;
      }
      Log.LogWrite("Spawning explode VFX\n");
      Log.LogWrite(" Range:"+Range+" Damage:"+AoEDmg+"\n");
      List<AoEComponentExplosionRecord> AoEDamage = new List<AoEComponentExplosionRecord>();
      List<EffectData> effects = component.AoEExplosionEffects();
      int SequenceID = component.parent.Combat.StackManager.NextStackUID;
      foreach (ICombatant target in component.parent.Combat.GetAllLivingCombatants()) {
        if (target.GUID == component.parent.GUID) { continue; };
        if (target.IsDead) { continue; };
        Vector3 CurrentPosition = target.CurrentPosition + Vector3.up * target.AoEHeightFix();
        float distance = Vector3.Distance(CurrentPosition, component.parent.CurrentPosition);
        if (distance > Range) { continue; };
        float HeatDamage = component.AoEExplodeHeat() * (Range - distance) / Range;
        float Damage = AoEDmg * (Range - distance) / Range;
        float StabDamage = component.AoEExplodeStability() * (Range - distance) / Range;
        foreach (EffectData effect in effects) {
          string effectID = string.Format("OnComponentAoEExplosionEffect_{0}_{1}", (object)component.parent.GUID, (object)SequenceID);
          Log.LogWrite($"  Applying effectID:{effect.Description.Id} with effectDescId:{effect?.Description.Id} effectDescName:{effect?.Description.Name}\n");
          component.parent.Combat.EffectManager.CreateEffect(effect, effectID, -1, component.parent, target, new WeaponHitInfo(), 0, false);
        }
        Mech mech = target as Mech;
        Vehicle vehicle = target as Vehicle;
        if (mech == null) {
          Damage += HeatDamage;
        };
        List<int> hitLocations = null;
        Dictionary<int, float> AOELocationDict = null;
        if (mech != null) {
          hitLocations = component.parent.Combat.HitLocation.GetPossibleHitLocations(component.parent.CurrentPosition, mech);
          if (AoEComponentExplodeHelper.MechHitLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.MechHitLocations;
          int HeadIndex = hitLocations.IndexOf((int)ArmorLocation.Head);
          if ((HeadIndex >= 0) && (HeadIndex < hitLocations.Count)) { hitLocations.RemoveAt(HeadIndex); };
        } else
        if (target is Vehicle) {
          hitLocations = component.parent.Combat.HitLocation.GetPossibleHitLocations(component.parent.CurrentPosition, target as Vehicle);
          if (AoEComponentExplodeHelper.VehicleLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.VehicleLocations;
        } else {
          hitLocations = new List<int>() { 1 };
          if (AoEComponentExplodeHelper.OtherLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.OtherLocations;
        }
        float fullLocationDamage = 0.0f;
        foreach (int hitLocation in hitLocations) {
          if (AOELocationDict.ContainsKey(hitLocation)) {
            fullLocationDamage += AOELocationDict[hitLocation];
          } else {
            fullLocationDamage += 100f;
          }
        }
        Log.LogWrite(" hitLocations: ");
        foreach (int hitLocation in hitLocations) {
          Log.LogWrite(" " + hitLocation);
        }
        Log.LogWrite("\n");
        Log.LogWrite(" full location damage coeff " + fullLocationDamage + "\n");
        AoEComponentExplosionRecord AoERecord = new AoEComponentExplosionRecord(target);
        AoERecord.HeatDamage = HeatDamage;
        AoERecord.StabDamage = StabDamage;
        foreach (int hitLocation in hitLocations) {
          float currentDamageCoeff = 100f;
          if (AOELocationDict.ContainsKey(hitLocation)) {
            currentDamageCoeff = AOELocationDict[hitLocation];
          }
          currentDamageCoeff /= fullLocationDamage;
          float CurrentLocationDamage = Damage * currentDamageCoeff;
          if(AoERecord.hitRecords.ContainsKey(hitLocation)) {
            AoERecord.hitRecords[hitLocation].Damage += CurrentLocationDamage;
          } else {
            Vector3 pos = target.getImpactPositionSimple(component.parent, component.parent.CurrentPosition, hitLocation);
            AoERecord.hitRecords[hitLocation] = new AoEComponentExplosionHitRecord(pos, CurrentLocationDamage);
          }
          Log.LogWrite("  location " + hitLocation + " damage " + AoERecord.hitRecords[hitLocation].Damage + "\n");
        }
        AoEDamage.Add(AoERecord);
      }
      Log.LogWrite("AoE Damage result:\n");
      Weapon fakeWeapon = new Weapon();
      fakeWeapon.parent = component.parent;
      typeof(MechComponent).GetProperty("componentDef", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true).Invoke(fakeWeapon,new object[1] { (object)component.componentDef });
      component.applyExplodeBurn(fakeWeapon);
      AoETempDesignMaskData tempMaskDeligate = new AoETempDesignMaskData(component, fakeWeapon);
      Thread TempMaskApplier = new Thread(new ThreadStart(tempMaskDeligate.procTempDesignMask));
      TempMaskApplier.Start(); 
      //component.applyImpactTempMask(fakeWeapon);
      component.AoEPlayExplodeVFX();
      CustAmmoCategories.CustomAudioSource explodeSound = component.AoEExplodeSound();
      if (explodeSound != null) {
        Log.LogWrite(" play explode sound\n");
        explodeSound.play(component.parent.GameRep.audioObject);
      }
      var fakeHit = new WeaponHitInfo(-1, -1, -1, -1, component.parent.GUID, component.parent.GUID, -1, null, null, null, null, null, null, new AttackImpactQuality[1] { AttackImpactQuality.Solid }, new AttackDirection[1] { AttackDirection.FromArtillery }, null, null, null);
      for (int index = 0; index < AoEDamage.Count; ++index) {
        Log.LogWrite(" "+ AoEDamage[index].target.DisplayName+":"+ AoEDamage[index].target.GUID+"\n");
        Log.LogWrite(" Heat:" + AoEDamage[index].HeatDamage+ "\n");
        Log.LogWrite(" Instability:" + AoEDamage[index].StabDamage + "\n");
        fakeHit.targetId = AoEDamage[index].target.GUID;
        foreach (var AOEHitRecord in AoEDamage[index].hitRecords) {
          Log.LogWrite("  location:" + AOEHitRecord.Key + " pos:" + AOEHitRecord.Value.hitPosition + " dmg:" + AOEHitRecord.Value.Damage + "\n");
          float LocArmor = AoEDamage[index].target.ArmorForLocation(AOEHitRecord.Key);
          if ((double)LocArmor < (double)AOEHitRecord.Value.Damage) {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, AoEDamage[index].target.GUID, new Text("{0}", new object[1]
            {
                      (object) (int) Mathf.Max(1f, AOEHitRecord.Value.Damage)
            }), component.parent.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.StructureDamage, AOEHitRecord.Value.hitPosition.x, AOEHitRecord.Value.hitPosition.y, AOEHitRecord.Value.hitPosition.z));
          } else {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, AoEDamage[index].target.GUID, new Text("{0}", new object[1]
            {
                      (object) (int) Mathf.Max(1f, AOEHitRecord.Value.Damage)
            }), component.parent.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.ArmorDamage, AOEHitRecord.Value.hitPosition.x, AOEHitRecord.Value.hitPosition.y, AOEHitRecord.Value.hitPosition.z));
          }
#if BT1_8
          AoEDamage[index].target.TakeWeaponDamage(fakeHit,AOEHitRecord.Key,fakeWeapon, AOEHitRecord.Value.Damage, 0f,0,DamageType.AmmoExplosion);
#else
          AoEDamage[index].target.TakeWeaponDamage(fakeHit, AOEHitRecord.Key, fakeWeapon, AOEHitRecord.Value.Damage, 0, DamageType.AmmoExplosion);
#endif
        }
        AoEDamage[index].target.HandleDeath(component.parent.GUID);
        Mech mech = AoEDamage[index].target as Mech;
        if(mech != null) {
          if (AoEDamage[index].HeatDamage > Core.Epsilon) {
            mech.AddExternalHeat("AoE Component explosion", Mathf.RoundToInt(AoEDamage[index].HeatDamage));
            mech.GenerateAndPublishHeatSequence(-1, true, false, component.parent.GUID);
          }
          if (AoEDamage[index].StabDamage > Core.Epsilon) {
            mech.AddAbsoluteInstability(AoEDamage[index].StabDamage, StabilityChangeSource.Effect, component.parent.GUID);
          }
          mech.HandleKnockdown(-1, component.parent.GUID, Vector2.one, (SequenceFinished)null);
        }
      }
    }
  }
}