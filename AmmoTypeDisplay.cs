using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace AmmoTypeDisplay {
	public class AmmoDisplayConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static AmmoDisplayConfig Instance;
		[DefaultValue(true)]
		[Label("Show tooltips on weapons/tools")]
		public bool consumerTooltips;

		[DefaultValue(false)]
		[Label("Show current ammo tooltip on weapons/tools")]
		public bool consumerCurrent;

		[DefaultValue(true)]
		[Label("Show tooltips on ammo")]
		public bool ammoTooltips;


		[DefaultValue(true)]
		[Label("Highlight ammo on hover")]
		public bool highlightAmmo;

		[DefaultValue(true)]
		[Label("Highlight weapons/tools on hover")]
		public bool highlightConsumer;
	}
	public class AmmoTypeDisplay : Mod {
		public static Dictionary<int, (string singular, string plural)> AmmoNames { get; private set; }
		public static HashSet<int> AlreadyDisplayingAmmo { get; private set; }
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
		public override void Load() {
			On.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ItemSlot_Draw;
		}
		public override void Unload() {
			On.Terraria.UI.ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color -= ItemSlot_Draw;
			DisplaySystem.hoverItem = null;
			DisplaySystem.context = 0;
			AmmoNames = null;
			AlreadyDisplayingAmmo = null;
		}
		private static void ItemSlot_Draw(On.Terraria.UI.ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
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
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (item.useAmmo > 0 && !AmmoTypeDisplay.AlreadyDisplayingAmmo.Contains(item.type)) {
				if (!AmmoDisplayConfig.Instance.consumerTooltips) return;
				if (AmmoDisplayConfig.Instance.consumerCurrent) {
					Main.LocalPlayer.PickAmmo(item, out _, out _, out _, out _, out int id, true);
					tooltips.Add(new TooltipLine(Mod, "AmmoType", $"[i:{id}] Using {Lang.GetItemName(id)}"));
					return;
				}
				string ammoName;
				if (AmmoTypeDisplay.AmmoNames.TryGetValue(item.useAmmo, out var ammoNames)) {
					ammoName = ammoNames.plural;
				} else {
					ammoName = Lang.GetItemName(item.useAmmo).ToString();
				}
				tooltips.Add(new TooltipLine(Mod, "AmmoType", $"[i:{item.useAmmo}] Uses {ammoName}"));
			} else if (item.ammo > 0 && AmmoDisplayConfig.Instance.ammoTooltips) {
				string ammoName = Lang.GetItemName(item.ammo).ToString();
				bool display = item.ammo != item.type;
				if (AmmoTypeDisplay.AmmoNames.TryGetValue(item.ammo, out var tempAmmoName)) {
					ammoName = tempAmmoName.singular;
					display = true;
				}
				if (display) {
					tooltips.Add(new TooltipLine(Mod, "AmmoType", $"[i:{item.ammo}] Counts as {ammoName}"));
				}
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
					(item.useAmmo > 0 && AmmoDisplayConfig.Instance.highlightConsumer && DisplaySystem.hoverItem?.ammo == item.useAmmo)) {
					spriteBatch.Draw(
						TextureAssets.InventoryBack13.Value,
						position - (new Vector2(52) * Main.inventoryScale) / 2f + frame.Size() * scale / 2f,
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