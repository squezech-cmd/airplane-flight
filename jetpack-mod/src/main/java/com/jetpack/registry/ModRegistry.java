package com.jetpack.registry;

import com.jetpack.JetpackItem;
import com.jetpack.JetpackMod;
import com.jetpack.block.BigChristmasTreeBlock;
import com.jetpack.block.ChristmasTreeBlock;
import com.jetpack.block.DecorBlock;
import com.jetpack.block.GiantChristmasTreeBlock;
import com.jetpack.block.FairyLightBlock;
import com.jetpack.block.GiftBlock;
import com.jetpack.block.HollyBushBlock;
import com.jetpack.block.SnowPileBlock;
import com.jetpack.block.SnowmanBlock;
import com.jetpack.block.WreathBlock;
import com.jetpack.item.FestiveFireworkItem;
import com.jetpack.item.HotCocoaItem;
import com.jetpack.item.SantaHatItem;
import com.jetpack.item.SnowGlobeItem;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.item.CreativeModeTab;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Rarity;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.RotatedPillarBlock;
import net.minecraft.world.level.block.SoundType;
import net.minecraft.world.level.block.state.BlockBehaviour;
import net.minecraft.world.level.material.MapColor;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

import java.util.function.Supplier;

/**
 * Every item, block and creative tab this mod adds.
 */
public final class ModRegistry {
    public static final DeferredRegister<Item> ITEMS =
            DeferredRegister.create(ForgeRegistries.ITEMS, JetpackMod.MOD_ID);
    public static final DeferredRegister<Block> BLOCKS =
            DeferredRegister.create(ForgeRegistries.BLOCKS, JetpackMod.MOD_ID);
    public static final DeferredRegister<CreativeModeTab> TABS =
            DeferredRegister.create(BuiltInRegistries.CREATIVE_MODE_TAB.key(), JetpackMod.MOD_ID);

    // ------------------------------------------------------------------ items

    public static final RegistryObject<Item> JETPACK = ITEMS.register("jetpack", JetpackItem::new);

    public static final RegistryObject<Item> CHRISTMAS_STAR = ITEMS.register("christmas_star",
            () -> new Item(new Item.Properties().rarity(Rarity.UNCOMMON).fireResistant()));

    public static final RegistryObject<Item> SNOW_GLOBE = ITEMS.register("snow_globe",
            () -> new SnowGlobeItem(new Item.Properties().stacksTo(1).rarity(Rarity.UNCOMMON)));

    public static final RegistryObject<Item> FESTIVE_FIREWORK = ITEMS.register("festive_firework",
            () -> new FestiveFireworkItem(new Item.Properties().stacksTo(16)));

    public static final RegistryObject<Item> HOT_COCOA = ITEMS.register("hot_cocoa",
            () -> new HotCocoaItem(new Item.Properties().stacksTo(1)));

    public static final RegistryObject<Item> SANTA_HAT = ITEMS.register("santa_hat",
            () -> new SantaHatItem(new Item.Properties().stacksTo(1).rarity(Rarity.RARE)));

    // ----------------------------------------------------------------- blocks

    public static final RegistryObject<Block> CHRISTMAS_TREE = registerBlock("christmas_tree",
            () -> new ChristmasTreeBlock(BlockBehaviour.Properties.of()
                    .mapColor(MapColor.PLANT)
                    .strength(1.0F)
                    .sound(SoundType.GRASS)
                    .lightLevel(state -> 8)
                    .noOcclusion()));

    public static final RegistryObject<Block> BIG_CHRISTMAS_TREE = registerBlock("big_christmas_tree",
            () -> new BigChristmasTreeBlock(BlockBehaviour.Properties.of()
                    .mapColor(MapColor.PLANT)
                    .strength(1.2F)
                    .sound(SoundType.GRASS)
                    .lightLevel(state -> 12)
                    .noOcclusion()));

    public static final RegistryObject<Block> GIANT_CHRISTMAS_TREE = registerBlock("giant_christmas_tree",
            () -> new GiantChristmasTreeBlock(BlockBehaviour.Properties.of()
                    .mapColor(MapColor.PLANT)
                    .strength(1.4F)
                    .sound(SoundType.GRASS)
                    .lightLevel(state -> 14)
                    .noOcclusion()));

    // ---- decorative furniture: these used to be inventory-only items ----

    public static final RegistryObject<Block> TOY_TRAIN_BLOCK = registerDecor("toy_train_block",
            DecorBlock.box(0, 0, 2.6, 16, 8.2, 13.4), DecorBlock.Flavour.SPARKLE, 0);
    public static final RegistryObject<Block> DRUM_BLOCK = registerDecor("drum_block",
            DecorBlock.box(2.4, 0.9, 3.1, 13.6, 13.5, 12.9), DecorBlock.Flavour.NONE, 0);
    public static final RegistryObject<Block> ROCKING_HORSE_BLOCK = registerDecor("rocking_horse_block",
            DecorBlock.box(1, 0.3, 4, 15.8, 15.8, 12), DecorBlock.Flavour.NONE, 0);
    public static final RegistryObject<Block> NUTCRACKER_BLOCK = registerDecor("nutcracker_block",
            DecorBlock.box(3.6, 0, 4.6, 13, 16, 11.4), DecorBlock.Flavour.DUST, 3);
    public static final RegistryObject<Block> GINGERBREAD_MAN_BLOCK = registerDecor("gingerbread_man_block",
            DecorBlock.box(0.5, 0.2, 5.2, 15.9, 15.05, 10.7), DecorBlock.Flavour.NONE, 0);
    public static final RegistryObject<Block> GINGERBREAD_HOUSE_BLOCK = registerDecor("gingerbread_house_block",
            DecorBlock.box(0.6, 0, 1.4, 15.4, 12.9, 13.8), DecorBlock.Flavour.DUST, 4);
    public static final RegistryObject<Block> STOCKING_BLOCK = registerDecor("stocking_block",
            DecorBlock.box(4.2, 0, 5, 15.2, 16, 11.8), DecorBlock.Flavour.NONE, 0);
    public static final RegistryObject<Block> MITTEN_BLOCK = registerDecor("mitten_block",
            DecorBlock.box(4.4, 0, 4.6, 13.6, 15.2, 11.4), DecorBlock.Flavour.NONE, 0);
    public static final RegistryObject<Block> BELL_BLOCK = registerDecor("bell_block",
            DecorBlock.box(2.05, 0, 2.05, 13.95, 13, 13.95), DecorBlock.Flavour.SPARKLE, 7);
    public static final RegistryObject<Block> BAUBLE_PILE_BLOCK = registerDecor("bauble_pile_block",
            DecorBlock.box(0.9, 0, 5.3, 14.3, 12.5, 14.9), DecorBlock.Flavour.GLITTER, 0);
    public static final RegistryObject<Block> CANDY_JAR_BLOCK = registerDecor("candy_jar_block",
            DecorBlock.box(3.5, 0, 3.3, 12.5, 13.6, 12.5), DecorBlock.Flavour.SPARKLE, 0);

    public static final RegistryObject<Block> GIFT_RED = registerBlock("gift_red",
            () -> new GiftBlock(BlockBehaviour.Properties.copy(Blocks.WHITE_WOOL)
                    .mapColor(MapColor.COLOR_RED)
                    .strength(0.6F)
                    .sound(SoundType.WOOL),
                    new int[] {0xE23B4E, 0xFFD25E}));

    public static final RegistryObject<Block> GIFT_GREEN = registerBlock("gift_green",
            () -> new GiftBlock(BlockBehaviour.Properties.copy(Blocks.WHITE_WOOL)
                    .mapColor(MapColor.COLOR_GREEN)
                    .strength(0.6F)
                    .sound(SoundType.WOOL),
                    new int[] {0x2E9B4F, 0xF2F2F2}));

    public static final RegistryObject<Block> GIFT_GOLD = registerBlock("gift_gold",
            () -> new GiftBlock(BlockBehaviour.Properties.copy(Blocks.WHITE_WOOL)
                    .mapColor(MapColor.GOLD)
                    .strength(0.6F)
                    .sound(SoundType.WOOL),
                    new int[] {0xE8B23C, 0xC0392B}));

    public static final RegistryObject<Block> SNOW_LANTERN = registerBlock("snow_lantern",
            () -> new Block(BlockBehaviour.Properties.copy(Blocks.GLASS)
                    .mapColor(MapColor.SNOW)
                    .strength(0.4F)
                    .sound(SoundType.GLASS)
                    .lightLevel(state -> 15)
                    .noOcclusion()));

    public static final RegistryObject<Block> CANDY_CANE = registerBlock("candy_cane",
            () -> new RotatedPillarBlock(BlockBehaviour.Properties.copy(Blocks.QUARTZ_BLOCK)
                    .mapColor(MapColor.COLOR_RED)
                    .strength(0.8F)
                    .sound(SoundType.BONE_BLOCK)));

    public static final RegistryObject<Block> SNOWMAN = registerBlock("snowman",
            () -> new SnowmanBlock(BlockBehaviour.Properties.copy(Blocks.SNOW_BLOCK)
                    .mapColor(MapColor.SNOW)
                    .strength(0.5F)
                    .sound(SoundType.SNOW)
                    .noOcclusion()));

    public static final RegistryObject<Block> WREATH = registerBlock("wreath",
            () -> new WreathBlock(BlockBehaviour.Properties.copy(Blocks.OAK_LEAVES)
                    .mapColor(MapColor.PLANT)
                    .strength(0.3F)
                    .sound(SoundType.GRASS)
                    .lightLevel(state -> 6)
                    .noOcclusion()));

    public static final RegistryObject<Block> FAIRY_LIGHTS = registerBlock("fairy_lights",
            () -> new FairyLightBlock(BlockBehaviour.Properties.copy(Blocks.OAK_LEAVES)
                    .mapColor(MapColor.PLANT)
                    .strength(0.2F)
                    .sound(SoundType.GRASS)
                    .lightLevel(state -> 11)
                    .noOcclusion()));

    public static final RegistryObject<Block> HOLLY_BUSH = registerBlock("holly_bush",
            () -> new HollyBushBlock(BlockBehaviour.Properties.copy(Blocks.OAK_LEAVES)
                    .mapColor(MapColor.PLANT)
                    .strength(0.4F)
                    .sound(SoundType.GRASS)
                    .noOcclusion()));

    public static final RegistryObject<Block> SNOW_PILE = registerBlock("snow_pile",
            () -> new SnowPileBlock(BlockBehaviour.Properties.copy(Blocks.SNOW_BLOCK)
                    .mapColor(MapColor.SNOW)
                    .strength(0.3F)
                    .sound(SoundType.SNOW)
                    .noOcclusion()));

    // ------------------------------------------------------------------- tabs

    public static final RegistryObject<CreativeModeTab> JETPACK_TAB = TABS.register("jetpack_tab", () ->
            CreativeModeTab.builder()
                    .title(Component.translatable("itemGroup.jetpack"))
                    .icon(() -> new ItemStack(JETPACK.get()))
                    .displayItems((params, output) -> output.accept(JETPACK.get()))
                    .build());

    public static final RegistryObject<CreativeModeTab> CHRISTMAS_TAB = TABS.register("christmas_tab", () ->
            CreativeModeTab.builder()
                    .title(Component.translatable("itemGroup.jetpack.christmas"))
                    .icon(() -> new ItemStack(CHRISTMAS_TREE.get()))
                    .displayItems((params, output) -> {
                        // Blocks first, in a tidy grid.
                        output.accept(GIANT_CHRISTMAS_TREE.get());
                        output.accept(BIG_CHRISTMAS_TREE.get());
                        output.accept(CHRISTMAS_TREE.get());
                        output.accept(SNOWMAN.get());
                        output.accept(WREATH.get());
                        output.accept(FAIRY_LIGHTS.get());
                        output.accept(HOLLY_BUSH.get());
                        output.accept(SNOW_PILE.get());
                        output.accept(CANDY_CANE.get());
                        output.accept(SNOW_LANTERN.get());
                        output.accept(GIFT_RED.get());
                        output.accept(GIFT_GREEN.get());
                        output.accept(GIFT_GOLD.get());

                        // Decorative furniture.
                        output.accept(TOY_TRAIN_BLOCK.get());
                        output.accept(DRUM_BLOCK.get());
                        output.accept(ROCKING_HORSE_BLOCK.get());
                        output.accept(NUTCRACKER_BLOCK.get());
                        output.accept(GINGERBREAD_MAN_BLOCK.get());
                        output.accept(GINGERBREAD_HOUSE_BLOCK.get());
                        output.accept(STOCKING_BLOCK.get());
                        output.accept(MITTEN_BLOCK.get());
                        output.accept(BELL_BLOCK.get());
                        output.accept(BAUBLE_PILE_BLOCK.get());
                        output.accept(CANDY_JAR_BLOCK.get());

                        // Signature items.
                        output.accept(CHRISTMAS_STAR.get());
                        output.accept(SNOW_GLOBE.get());
                        output.accept(FESTIVE_FIREWORK.get());
                        output.accept(HOT_COCOA.get());
                        output.accept(SANTA_HAT.get());

                        // Then the whole Christmas catalogue.
                        ChristmasItems.DECORATIONS.forEach(item -> output.accept(item.get()));
                        ChristmasItems.SWEETS.forEach(item -> output.accept(item.get()));
                        ChristmasItems.DRINKS.forEach(item -> output.accept(item.get()));
                        ChristmasItems.TRINKETS.forEach(item -> output.accept(item.get()));
                        ChristmasItems.TOOLS.forEach(item -> output.accept(item.get()));
                        ChristmasItems.MATERIALS.forEach(item -> output.accept(item.get()));
                    })
                    .build());

    private ModRegistry() {
    }

    /** Registers a decoration block plus its item, with an optional glow. */
    private static RegistryObject<Block> registerDecor(String name,
                                                       net.minecraft.world.phys.shapes.VoxelShape shape,
                                                       DecorBlock.Flavour flavour, int light) {
        RegistryObject<Block> registered = BLOCKS.register(name, () -> new DecorBlock(
                BlockBehaviour.Properties.of()
                        .mapColor(MapColor.WOOD)
                        .strength(0.5F)
                        .sound(flavour == DecorBlock.Flavour.NONE ? SoundType.WOOD : SoundType.AMETHYST)
                        .lightLevel(state -> light)
                        .noOcclusion(),
                shape, flavour));
        ITEMS.register(name, () -> new BlockItem(registered.get(), new Item.Properties()));
        return registered;
    }

    /** Registers a block together with its matching block item. */
    private static <T extends Block> RegistryObject<T> registerBlock(String name, Supplier<T> block) {
        RegistryObject<T> registered = BLOCKS.register(name, block);
        ITEMS.register(name, () -> new BlockItem(registered.get(), new Item.Properties()));
        return registered;
    }

    public static void register(IEventBus bus) {
        // Touch the class so its static fields run before registration.
        com.jetpack.JetpackMod.LOGGER.debug("Registering {} Christmas items", ChristmasItems.count());
        BLOCKS.register(bus);
        ITEMS.register(bus);
        TABS.register(bus);
    }
}
