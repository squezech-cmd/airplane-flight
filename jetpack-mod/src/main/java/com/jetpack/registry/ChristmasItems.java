package com.jetpack.registry;

import com.jetpack.item.EffectItem;
import com.jetpack.item.FrostSwordItem;
import com.jetpack.item.TooltipItem;
import net.minecraft.ChatFormatting;
import net.minecraft.world.effect.MobEffects;
import net.minecraft.world.food.FoodProperties;
import net.minecraft.world.item.AxeItem;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.PickaxeItem;
import net.minecraft.world.item.Rarity;
import net.minecraft.world.item.ShovelItem;
import net.minecraftforge.registries.RegistryObject;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Supplier;

import static com.jetpack.registry.ModRegistry.ITEMS;

/**
 * The Christmas catalogue: 52 collectible, edible, drinkable and wearable items
 * on top of the jetpack itself. Everything is grouped so the creative tab and
 * the texture generator can walk the same lists.
 */
public final class ChristmasItems {

    public static final List<RegistryObject<? extends Item>> DECORATIONS = new ArrayList<>();
    public static final List<RegistryObject<? extends Item>> SWEETS = new ArrayList<>();
    public static final List<RegistryObject<? extends Item>> DRINKS = new ArrayList<>();
    public static final List<RegistryObject<? extends Item>> TRINKETS = new ArrayList<>();
    public static final List<RegistryObject<? extends Item>> TOOLS = new ArrayList<>();
    public static final List<RegistryObject<? extends Item>> MATERIALS = new ArrayList<>();

    // ------------------------------------------------------------ decorations

    public static final RegistryObject<Item> SNOWFLAKE = decor("snowflake", Rarity.COMMON,
            ChatFormatting.AQUA, "A perfect six-armed crystal.");
    public static final RegistryObject<Item> BELL = decor("bell", Rarity.COMMON,
            ChatFormatting.GOLD, "Rings when the wind picks up.");
    public static final RegistryObject<Item> TINSEL = decor("tinsel", Rarity.COMMON,
            ChatFormatting.LIGHT_PURPLE, "Shiny strands for the tree.");
    public static final RegistryObject<Item> STRING_LIGHTS = decor("string_lights", Rarity.COMMON,
            ChatFormatting.YELLOW, "A string of tiny warm bulbs.");
    public static final RegistryObject<Item> GINGERBREAD_MAN = decor("gingerbread_man", Rarity.COMMON,
            ChatFormatting.GOLD, "Baked with extra icing buttons.");
    public static final RegistryObject<Item> GINGERBREAD_HOUSE = decor("gingerbread_house", Rarity.UNCOMMON,
            ChatFormatting.GOLD, "A whole house of gingerbread.");
    public static final RegistryObject<Item> NUTCRACKER = decor("nutcracker", Rarity.UNCOMMON,
            ChatFormatting.RED, "Stands guard by the fireplace.");
    public static final RegistryObject<Item> ROCKING_HORSE = decor("rocking_horse", Rarity.COMMON,
            ChatFormatting.GOLD, "A hand-carved wooden toy.");
    public static final RegistryObject<Item> TOY_TRAIN = decor("toy_train", Rarity.UNCOMMON,
            ChatFormatting.DARK_RED, "Chuffs happily around the tree.");
    public static final RegistryObject<Item> DRUM = decor("drum", Rarity.COMMON,
            ChatFormatting.RED, "A little tin drum.");
    public static final RegistryObject<Item> STOCKING = decor("stocking", Rarity.COMMON,
            ChatFormatting.RED, "Hung by the chimney with care.");
    public static final RegistryObject<Item> MITTEN = decor("mitten", Rarity.COMMON,
            ChatFormatting.RED, "Knitted by a very patient grandmother.");
    public static final RegistryObject<Item> BAUBLE_RED = decor("bauble_red", Rarity.COMMON,
            ChatFormatting.RED, "A glass bauble, ruby red.");
    public static final RegistryObject<Item> BAUBLE_GOLD = decor("bauble_gold", Rarity.COMMON,
            ChatFormatting.GOLD, "A glass bauble, spun gold.");
    public static final RegistryObject<Item> BAUBLE_BLUE = decor("bauble_blue", Rarity.COMMON,
            ChatFormatting.AQUA, "A glass bauble, ice blue.");
    public static final RegistryObject<Item> STAR_GARLAND = decor("star_garland", Rarity.UNCOMMON,
            ChatFormatting.YELLOW, "Tiny stars threaded on a silver wire.");

    // ---------------------------------------------------------------- sweets

    public static final RegistryObject<Item> PEPPERMINT_CANDY = sweet("peppermint_candy", 2, 0.3F,
            ChatFormatting.AQUA, "Cool and sharp.", null, 0);
    public static final RegistryObject<Item> CANDY_APPLE = sweet("candy_apple", 4, 0.4F,
            ChatFormatting.RED, "Glossy red candy shell.", null, 0);
    public static final RegistryObject<Item> CHOCOLATE_BAR = sweet("chocolate_bar", 5, 0.5F,
            ChatFormatting.DARK_RED, "Wrapped in gold foil.", null, 0);
    public static final RegistryObject<Item> GINGERBREAD_COOKIE = sweet("gingerbread_cookie", 4, 0.5F,
            ChatFormatting.GOLD, "Warm spice and icing.", null, 0);
    public static final RegistryObject<Item> CHRISTMAS_PUDDING = sweet("christmas_pudding", 7, 0.6F,
            ChatFormatting.GOLD, "Best served flaming.", null, 0);
    public static final RegistryObject<Item> FRUITCAKE = sweet("fruitcake", 8, 0.7F,
            ChatFormatting.GOLD, "Dense, ancient and beloved.", null, 0);
    public static final RegistryObject<Item> ROASTED_CHESTNUTS = sweet("roasted_chestnuts", 6, 0.6F,
            ChatFormatting.GOLD, "Chestnuts roasting on an open fire.",
            () -> MobEffects.MOVEMENT_SPEED, 20 * 20);
    public static final RegistryObject<Item> MARSHMALLOW = sweet("marshmallow", 3, 0.3F,
            ChatFormatting.WHITE, "Squishy, vanilla-scented.", null, 0);
    public static final RegistryObject<Item> CHRISTMAS_CAKE = sweet("christmas_cake", 10, 0.9F,
            ChatFormatting.RED, "Layered and generous.", () -> MobEffects.REGENERATION, 20 * 6);
    public static final RegistryObject<Item> SUGAR_PLUM = sweet("sugar_plum", 5, 0.6F,
            ChatFormatting.LIGHT_PURPLE, "Dances in your head.", () -> MobEffects.NIGHT_VISION, 20 * 20);
    public static final RegistryObject<Item> SNOW_CONE = sweet("snow_cone", 3, 0.3F,
            ChatFormatting.AQUA, "Shaved ice in a paper cone.", () -> MobEffects.FIRE_RESISTANCE, 20 * 8);
    public static final RegistryObject<Item> SWEET_CANE = sweet("sweet_cane", 3, 0.4F,
            ChatFormatting.RED, "Striped and sweet.", null, 0);
    public static final RegistryObject<Item> MARZIPAN = sweet("marzipan", 5, 0.6F,
            ChatFormatting.GOLD, "Shaped like a tiny pig.", null, 0);
    public static final RegistryObject<Item> YULE_LOG = sweet("yule_log", 9, 0.8F,
            ChatFormatting.DARK_RED, "A rolled chocolate log.", () -> MobEffects.DAMAGE_RESISTANCE, 20 * 12);

    // ---------------------------------------------------------------- drinks

    public static final RegistryObject<Item> EGGNOG = drink("eggnog", "Thick, rich and spiced.",
            () -> MobEffects.HEALTH_BOOST, 20 * 30);
    public static final RegistryObject<Item> MULLED_CIDER = drink("mulled_cider", "Apple and cinnamon.",
            () -> MobEffects.DAMAGE_RESISTANCE, 20 * 20);
    public static final RegistryObject<Item> PEPPERMINT_TEA = drink("peppermint_tea", "Breathes clear.",
            () -> MobEffects.NIGHT_VISION, 20 * 25);
    public static final RegistryObject<Item> CHRISTMAS_PUNCH = drink("christmas_punch", "Fruit and sparkle.",
            () -> MobEffects.HUNGER, 20 * 15);
    public static final RegistryObject<Item> SNOWBERRY_SMOOTHIE = drink("snowberry_smoothie", "Frozen berries.",
            () -> MobEffects.MOVEMENT_SPEED, 20 * 25);
    public static final RegistryObject<Item> WARM_COCOA = drink("warm_cocoa", "Topped with cream.",
            () -> MobEffects.REGENERATION, 20 * 10);

    // --------------------------------------------------------------- trinkets

    public static final RegistryObject<Item> ICE_SHARD = trinket("ice_shard", Rarity.COMMON,
            ChatFormatting.AQUA, "Splits the light into rainbows.");
    public static final RegistryObject<Item> FROZEN_HEART = trinket("frozen_heart", Rarity.RARE,
            ChatFormatting.LIGHT_PURPLE, "Still beating, somehow.");
    public static final RegistryObject<Item> SNOWY_PEARL = trinket("snowy_pearl", Rarity.UNCOMMON,
            ChatFormatting.WHITE, "Cool to the touch.");
    public static final RegistryObject<Item> LUCKY_SNOWBALL = trinket("lucky_snowball", Rarity.UNCOMMON,
            ChatFormatting.AQUA, "It has never once missed.");
    public static final RegistryObject<Item> STAR_DUST = trinket("star_dust", Rarity.RARE,
            ChatFormatting.YELLOW, "Scraped off the sky.");
    public static final RegistryObject<Item> MISTLETOE = trinket("mistletoe", Rarity.COMMON,
            ChatFormatting.GREEN, "You know what this means.");
    public static final RegistryObject<Item> HOLLY_BERRIES = trinket("holly_berries", Rarity.COMMON,
            ChatFormatting.DARK_RED, "Bright red against the snow.");
    public static final RegistryObject<Item> PINE_CONE = trinket("pine_cone", Rarity.COMMON,
            ChatFormatting.DARK_GREEN, "Smells of the forest.");

    // ------------------------------------------------------------------ tools

    public static final RegistryObject<Item> FROST_SWORD = tool("frost_sword",
            () -> new FrostSwordItem(new Item.Properties().rarity(Rarity.UNCOMMON)));
    public static final RegistryObject<Item> FROST_PICKAXE = tool("frost_pickaxe",
            () -> new PickaxeItem(com.jetpack.item.FrostTier.INSTANCE, 1, -2.8F,
                    new Item.Properties().rarity(Rarity.UNCOMMON)));
    public static final RegistryObject<Item> FROST_AXE = tool("frost_axe",
            () -> new AxeItem(com.jetpack.item.FrostTier.INSTANCE, 6.0F, -3.1F,
                    new Item.Properties().rarity(Rarity.UNCOMMON)));
    public static final RegistryObject<Item> FROST_SHOVEL = tool("frost_shovel",
            () -> new ShovelItem(com.jetpack.item.FrostTier.INSTANCE, 1.5F, -3.0F,
                    new Item.Properties().rarity(Rarity.UNCOMMON)));

    // -------------------------------------------------------------- materials

    public static final RegistryObject<Item> WRAPPING_PAPER = material("wrapping_paper",
            ChatFormatting.RED, "Printed with little stars.");
    public static final RegistryObject<Item> RED_BOW = material("red_bow",
            ChatFormatting.RED, "Tied with care.");
    public static final RegistryObject<Item> GOLD_BOW = material("gold_bow",
            ChatFormatting.GOLD, "For the important parcels.");
    public static final RegistryObject<Item> PINE_NEEDLES = material("pine_needles",
            ChatFormatting.DARK_GREEN, "Still faintly fragrant.");
    public static final RegistryObject<Item> SNOW_DUST = material("snow_dust",
            ChatFormatting.WHITE, "Sparkling powder.");

    private ChristmasItems() {
    }

    // ---------------------------------------------------------------- helpers

    private static RegistryObject<Item> decor(String name, Rarity rarity, ChatFormatting colour, String... lines) {
        RegistryObject<Item> item = ITEMS.register(name,
                () -> new TooltipItem(new Item.Properties().rarity(rarity), colour, lines));
        DECORATIONS.add(item);
        return item;
    }

    private static RegistryObject<Item> sweet(String name, int nutrition, float saturation,
                                              ChatFormatting colour, String line,
                                              Supplier<net.minecraft.world.effect.MobEffect> effect, int ticks) {
        RegistryObject<Item> item = ITEMS.register(name, () -> {
            FoodProperties.Builder builder = new FoodProperties.Builder()
                    .nutrition(nutrition)
                    .saturationMod(saturation);
            if (effect != null && ticks > 0) {
                builder.effect(new net.minecraft.world.effect.MobEffectInstance(effect.get(), ticks, 0), 1.0F);
            }
            return new TooltipItem(new Item.Properties().food(builder.build()), colour, line);
        });
        SWEETS.add(item);
        return item;
    }

    private static RegistryObject<Item> drink(String name, String line,
                                              Supplier<net.minecraft.world.effect.MobEffect> effect, int ticks) {
        RegistryObject<Item> item = ITEMS.register(name, () -> new EffectItem(
                new Item.Properties().stacksTo(1), ChatFormatting.GOLD, new String[] {line},
                effect, ticks, null, 0, true));
        DRINKS.add(item);
        return item;
    }

    private static RegistryObject<Item> trinket(String name, Rarity rarity, ChatFormatting colour, String... lines) {
        RegistryObject<Item> item = ITEMS.register(name,
                () -> new TooltipItem(new Item.Properties().rarity(rarity), colour, lines));
        TRINKETS.add(item);
        return item;
    }

    private static RegistryObject<Item> tool(String name, java.util.function.Supplier<Item> supplier) {
        RegistryObject<Item> item = ITEMS.register(name, supplier);
        TOOLS.add(item);
        return item;
    }

    private static RegistryObject<Item> material(String name, ChatFormatting colour, String... lines) {
        RegistryObject<Item> item = ITEMS.register(name,
                () -> new TooltipItem(new Item.Properties(), colour, lines));
        MATERIALS.add(item);
        return item;
    }

    public static int count() {
        return DECORATIONS.size() + SWEETS.size() + DRINKS.size() + TRINKETS.size()
                + TOOLS.size() + MATERIALS.size();
    }
}
