using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.UI;

namespace AmmoTypeDisplay {
	public class AmmoDisplayConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static AmmoDisplayConfig Instance;
		[DefaultValue(true)]
		public bool consumerTooltips;

		[DefaultValue(false)]
		public bool consumerCurrent;

		[DefaultValue(true)]
		public bool ammoTooltips;


		[DefaultValue(true)]
		public bool highlightAmmo;

		[DefaultValue(true)]
		public bool highlightConsumer;

		[DefaultValue("")]
		public string customWeaponFormatting;

		[DefaultValue("")]
		public string customWeaponUsesFormatting;

		[DefaultValue("")]
		public string customFishingRodFormatting;

		[DefaultValue("")]
		public string customAmmoFormatting;
		public override void OnChanged() {
			has_customWeaponFormatting = !string.IsNullOrWhiteSpace(customWeaponFormatting);
			has_customWeaponUsesFormatting = !string.IsNullOrWhiteSpace(customWeaponUsesFormatting);
			has_customFishingRodFormatting = !string.IsNullOrWhiteSpace(customFishingRodFormatting);
			has_customAmmoFormatting = !string.IsNullOrWhiteSpace(customAmmoFormatting);
		}
		[JsonIgnore]
		public bool has_customWeaponFormatting;
		[JsonIgnore]
		public bool has_customWeaponUsesFormatting;
		[JsonIgnore]
		public bool has_customFishingRodFormatting;
		[JsonIgnore]
		public bool has_customAmmoFormatting;
	}
	public class AmmoTypeDisplay : Mod {
		public static Dictionary<int, (string singular, string plural)> AmmoNames { get; private set; }
		public static HashSet<int> AlreadyDisplayingAmmo { get; private set; }
		public static bool isModifyingTooltips = false;
		public AmmoTypeDisplay() : base() {
			AmmoNames = new() {
				[AmmoID.Arrow] = ("arrow", "arrows"),
				[AmmoID.Bullet] = ("bullet", "bullets"),
				[AmmoID.Coin] = ("coins", "coins"),
				[AmmoID.Dart] = ("dart", "darts"),
				[AmmoID.Flare] = ("flare", "flares"),
			};
			AlreadyDisplayingAmmo = new() {
				ItemID.StarCannon,
				ItemID.Clentaminator
			};
		}
		public override object Call(params object[] args) {
			return args[0] switch {
				"AmmoNames" => AmmoNames,
				"AlreadyDisplayingAmmo" => AlreadyDisplayingAmmo,
				"isModifyingTooltips" => (Func<bool>)(() => isModifyingTooltips),
				_ => null
			};
		}
		public static LocalizedText itemTag;
		public override void Load() {
			On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ItemSlot_Draw;
			itemTag = Language.GetOrRegister("Mods.ShadedItemTag.TooltipTag", () => "i");
		}
		public override void Unload() {
			On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color -= ItemSlot_Draw;
			DisplaySystem.hoverItem = null;
			DisplaySystem.context = 0;
			AmmoNames = null;
			AlreadyDisplayingAmmo = null;
			itemTag = null;
		}
		private static void ItemSlot_Draw(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
			DisplaySystem.context = context;
			orig(spriteBatch, inv, context, slot, position, lightColor);
		}
	}
	public class DisplaySystem : ModSystem {
		internal static Item hoverItem;
		internal static int context;
		public override void PostDrawInterface(SpriteBatch spriteBatch) {
			switch (context) {
				case ItemSlot.Context.EquipMount:
				case ItemSlot.Context.EquipMinecart:
				case ItemSlot.Context.EquipPet:
				case ItemSlot.Context.EquipLight:
				case ItemSlot.Context.EquipGrapple:
				case ItemSlot.Context.CreativeInfinite:
				case ItemSlot.Context.ShopItem:
				case ItemSlot.Context.HotbarItem:
				case ItemSlot.Context.InventoryItem:
				case ItemSlot.Context.InventoryCoin:
				case ItemSlot.Context.InventoryAmmo:
				case ItemSlot.Context.ChestItem:
				case ItemSlot.Context.BankItem:
				case ItemSlot.Context.PrefixItem:
				case ItemSlot.Context.TrashItem:
				hoverItem = Main.HoverItem;
				break;
			}
		}
	}
	public class DisplayGlobalItem : GlobalItem {
		static string GetTooltip(string key, params object[] replacements) {
			return Language.GetTextValue("Mods.AmmoTypeDisplay." + key, replacements);
		}
		static string SubstituteWith(string text, Dictionary<string, string> replacements) {
			foreach (var item in replacements) {
				text = Regex.Replace(text, $"@{item.Key}\\b", item.Value);
			}
			return text;
		}
		static Dictionary<string, string> CreateSubstitutionDictionary(Item weapon, Item ammo) {
			Dictionary<string, string> dict = new();
			if (weapon is not null) {
				if (ammo is not null) {
					dict.Add("ammo", $"[{AmmoTypeDisplay.itemTag}/s{ammo.stack}:{ammo.type}]");
					dict.Add("ammoName", ammo.Name);
					dict.Add("ammoCount", ammo.stack.ToString());
					dict.Add("ammoRare", ammo.rare.ToString());
					Color rarityColor = Color.White;
					if (ammo.master) {
						rarityColor = new Color(255, (int)(Main.masterColor * 200f), 0);
					} else if (ammo.expert) {
						rarityColor = Main.DiscoColor;
					} else {
						switch (ammo.rare) {
							case ItemRarityID.Quest:		rarityColor = Colors.RarityAmber; break;
							case ItemRarityID.Gray:			rarityColor = Colors.RarityTrash; break;
							case ItemRarityID.White:		rarityColor = Colors.RarityNormal; break;
							case ItemRarityID.Blue:			rarityColor = Colors.RarityBlue; break;
							case ItemRarityID.Green:		rarityColor = Colors.RarityGreen; break;
							case ItemRarityID.Orange:		rarityColor = Colors.RarityOrange; break;
							case ItemRarityID.LightRed:		rarityColor = Colors.RarityRed; break;
							case ItemRarityID.Pink:			rarityColor = Colors.RarityPink; break;
							case ItemRarityID.LightPurple:	rarityColor = Colors.RarityPurple; break;
							case ItemRarityID.Lime:			rarityColor = Colors.RarityLime; break;
							case ItemRarityID.Yellow:		rarityColor = Colors.RarityYellow; break;
							case ItemRarityID.Cyan:			rarityColor = Colors.RarityCyan; break;
							case ItemRarityID.Red:			rarityColor = Colors.RarityDarkRed; break;
							case ItemRarityID.Purple:		rarityColor = Colors.RarityDarkPurple; break;
						}
					}
					dict.Add("ammoNameColored", $"[c/{rarityColor.Hex3()}:{ammo.Name}]");
				} else {
					dict.Add("ammo", $"[{AmmoTypeDisplay.itemTag}:{weapon.useAmmo}]");
					if (AmmoTypeDisplay.AmmoNames.TryGetValue(weapon.useAmmo, out var ammoNames)) {
						dict.Add("ammoName", ammoNames.plural);
					} else {
						dict.Add("ammoName", Lang.GetItemName(weapon.useAmmo).ToString());
					}
					dict.Add("ammoNameColored", dict["ammoName"]);
					dict.Add("ammoCount", "0");
					ammo = ContentSamples.ItemsByType[weapon.useAmmo];
				}
				dict.Add("ammoDamage", ammo.damage.ToString());
				dict.Add("ammoDamageClass", ammo.DamageType.DisplayName.Value);
				dict.Add("ammoCrit", ammo.crit.ToString());
				dict.Add("ammoKnockback", $"{ammo.knockBack:0.##}");
				dict.Add("ammoVelocity", $"{ammo.shootSpeed:0.##}");
				dict.Add("ammoVelocityMult", $"{ContentSamples.ProjectilesByType[ammo.shoot].MaxUpdates}");
				dict.Add("baitPower", ammo.bait.ToString());
			} else if (ammo is not null) {
				dict.Add("ammo", $"[{AmmoTypeDisplay.itemTag}:{ammo.ammo}]");
				if (AmmoTypeDisplay.AmmoNames.TryGetValue(ammo.ammo, out var ammoNames)) {
					dict.Add("ammoName", ammoNames.singular);
				} else {
					dict.Add("ammoName", Lang.GetItemName(ammo.ammo).ToString());
				}
				dict.Add("ammoVelocity", $"{ammo.shootSpeed:0.##}");
				dict.Add("ammoVelocityMult", $"{ContentSamples.ProjectilesByType[ammo.shoot].MaxUpdates}");
			}
			return dict;
		}
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			try {
				AmmoTypeDisplay.isModifyingTooltips = true;
				if (item.useAmmo > 0 && !AmmoTypeDisplay.AlreadyDisplayingAmmo.Contains(item.type)) {
					if (!AmmoDisplayConfig.Instance.consumerTooltips) return;
					if (AmmoDisplayConfig.Instance.consumerCurrent) {
						Item ammo = Main.LocalPlayer.ChooseAmmo(item);
						//tooltips.Add(new TooltipLine(Mod, "AmmoType", $"{ITag(id)} Using {Lang.GetItemName(id)}"));
						if (ammo is not null) {
							if (AmmoDisplayConfig.Instance.has_customWeaponFormatting) {
								tooltips.Add(new TooltipLine(Mod, "AmmoType", SubstituteWith(AmmoDisplayConfig.Instance.customWeaponFormatting, CreateSubstitutionDictionary(item, ammo))));
								return;
							}
							tooltips.Add(new TooltipLine(Mod, "AmmoType", GetTooltip("Using", ammo.type, ammo.stack, ammo.Name)));
							return;
						}
					}
					if (AmmoDisplayConfig.Instance.has_customWeaponUsesFormatting) {
						tooltips.Add(new TooltipLine(Mod, "AmmoType", SubstituteWith(AmmoDisplayConfig.Instance.customWeaponUsesFormatting, CreateSubstitutionDictionary(item, null))));
						return;
					}
					string ammoName;
					if (AmmoTypeDisplay.AmmoNames.TryGetValue(item.useAmmo, out var ammoNames)) {
						ammoName = ammoNames.plural;
					} else {
						ammoName = Lang.GetItemName(item.useAmmo).ToString();
					}
					tooltips.Add(new TooltipLine(Mod, "AmmoType", GetTooltip("Uses", item.useAmmo, ammoName)));
				} else if (item.fishingPole > 1 && AmmoDisplayConfig.Instance.consumerTooltips) {
					Item bait = null;
					Item[] inventory = Main.LocalPlayer.inventory;
					for (int i = 54; i >= 54 || i < 50; i++) {
						if (i >= 58) i = 0;
						if (inventory[i].stack > 0 && inventory[i].bait > 0) {
							bait = inventory[i];
							break;
						}
					}
					if (bait is not null) {
						if (AmmoDisplayConfig.Instance.has_customFishingRodFormatting) {
							tooltips.Add(new TooltipLine(Mod, "AmmoType", SubstituteWith(AmmoDisplayConfig.Instance.customFishingRodFormatting, CreateSubstitutionDictionary(item, bait))));
							return;
						}
						tooltips.Add(new TooltipLine(Mod, "AmmoType", GetTooltip("Using", bait.type, bait.stack, bait.Name)));
					}
				} else if (item.ammo > 0 && AmmoDisplayConfig.Instance.ammoTooltips) {
					string ammoName = Lang.GetItemName(item.ammo).ToString();
					bool display = item.ammo != item.type;
					if (AmmoTypeDisplay.AmmoNames.TryGetValue(item.ammo, out var tempAmmoName)) {
						ammoName = tempAmmoName.singular;
						display = true;
					}
					if (display) {
						if (AmmoDisplayConfig.Instance.has_customAmmoFormatting) {
							tooltips.Add(new TooltipLine(Mod, "AmmoType", SubstituteWith(AmmoDisplayConfig.Instance.customAmmoFormatting, CreateSubstitutionDictionary(null, item))));
							return;
						}
						tooltips.Add(new TooltipLine(Mod, "AmmoType", GetTooltip("CountsAs", item.ammo, ammoName)));
					}
				}
			} finally {
				AmmoTypeDisplay.isModifyingTooltips = false;
			}
		}
		public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			switch (DisplaySystem.context) {
				case ItemSlot.Context.CreativeInfinite:
				case ItemSlot.Context.ShopItem:
				case ItemSlot.Context.HotbarItem:
				case ItemSlot.Context.InventoryItem:
				case ItemSlot.Context.InventoryCoin:
				case ItemSlot.Context.InventoryAmmo:
				case ItemSlot.Context.ChestItem:
				case ItemSlot.Context.BankItem:
				case ItemSlot.Context.TrashItem:
				if (
					(item.ammo > 0 && AmmoDisplayConfig.Instance.highlightAmmo && DisplaySystem.hoverItem?.useAmmo == item.ammo) ||
					(item.bait > 0 && AmmoDisplayConfig.Instance.highlightAmmo && DisplaySystem.hoverItem?.fishingPole > 1) ||
					(item.useAmmo > 0 && AmmoDisplayConfig.Instance.highlightConsumer && DisplaySystem.hoverItem?.ammo == item.useAmmo)) {
					spriteBatch.Draw(
						TextureAssets.InventoryBack13.Value,
						position - (new Vector2(52) * Main.inventoryScale) / 2f,
						null,
						new Color(43, 185, 255, 200) * 0.333f,
						0,
						Vector2.Zero,
						Main.inventoryScale,
						0,
					0);
				}
				break;
			}
			return true;
		}
	}
}