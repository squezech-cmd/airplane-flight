package com.customnpcs.entity;

import com.customnpcs.registry.ModEntities;
import net.minecraft.core.BlockPos;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.network.syncher.EntityDataAccessor;
import net.minecraft.network.syncher.EntityDataSerializers;
import net.minecraft.network.syncher.SynchedEntityData;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvent;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.world.DifficultyInstance;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.EntityDimensions;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.MobSpawnType;
import net.minecraft.world.entity.PathfinderMob;
import net.minecraft.world.entity.Pose;
import net.minecraft.world.entity.SpawnGroupData;
import net.minecraft.world.entity.ai.attributes.AttributeSupplier;
import net.minecraft.world.entity.ai.attributes.Attributes;
import net.minecraft.world.entity.ai.goal.FloatGoal;
import net.minecraft.world.entity.ai.goal.LookAtPlayerGoal;
import net.minecraft.world.entity.ai.goal.MeleeAttackGoal;
import net.minecraft.world.entity.ai.goal.OpenDoorGoal;
import net.minecraft.world.entity.ai.goal.RandomLookAroundGoal;
import net.minecraft.world.entity.ai.goal.RandomStrollGoal;
import net.minecraft.world.entity.ai.goal.target.HurtByTargetGoal;
import net.minecraft.world.entity.ai.goal.target.NearestAttackableTargetGoal;
import net.minecraft.world.entity.ai.navigation.GroundPathNavigation;
import net.minecraft.world.entity.monster.Monster;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.ServerLevelAccessor;
import org.jetbrains.annotations.Nullable;

import java.util.Optional;
import java.util.UUID;

/**
 * A fully configurable humanoid character.
 *
 * <p>One entity class covers every NPC in the mod: the differences live in the
 * data, not in the class hierarchy. Name, skin, behaviour, dialogue, scale and
 * gear are all synced fields, so two players see the same NPC.
 *
 * <p>Editing happens through the spawner item and the NPC's own menu, which
 * sends the changes to the server; nothing is written to disk outside the
 * entity's NBT.
 */
public class CustomNpc extends PathfinderMob {
    // ---- synced data ------------------------------------------------------
    private static final EntityDataAccessor<String> DATA_NAME =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<String> DATA_SKIN =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Integer> DATA_BEHAVIOUR =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<Integer> DATA_SCALE =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.INT);
    private static final EntityDataAccessor<String> DATA_DIALOGUE =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.STRING);
    private static final EntityDataAccessor<Boolean> DATA_INVULNERABLE =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Boolean> DATA_HOSTILE =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.BOOLEAN);
    private static final EntityDataAccessor<Optional<UUID>> DATA_OWNER =
            SynchedEntityData.defineId(CustomNpc.class, EntityDataSerializers.OPTIONAL_UUID);

    /** Home position for wandering, set when the NPC is placed. */
    private BlockPos home;

    public CustomNpc(EntityType<? extends CustomNpc> type, Level level) {
        super(type, level);
        this.setPersistenceRequired();
        this.xpReward = 0;
        if (this.getNavigation() instanceof GroundPathNavigation navigation) {
            navigation.setCanOpenDoors(true);
            navigation.setCanPassDoors(true);
        }
    }

    public static AttributeSupplier.Builder createAttributes() {
        return PathfinderMob.createMobAttributes()
                .add(Attributes.MAX_HEALTH, 20.0D)
                .add(Attributes.MOVEMENT_SPEED, 0.28D)
                .add(Attributes.FOLLOW_RANGE, 32.0D)
                .add(Attributes.ATTACK_DAMAGE, 2.0D);
    }

    @Override
    protected void defineSynchedData() {
        super.defineSynchedData();
        this.entityData.define(DATA_NAME, "");
        this.entityData.define(DATA_SKIN, "customnpcs:default");
        this.entityData.define(DATA_BEHAVIOUR, NpcBehaviour.WANDER.ordinal());
        this.entityData.define(DATA_SCALE, 100);
        this.entityData.define(DATA_DIALOGUE, "");
        this.entityData.define(DATA_INVULNERABLE, true);
        this.entityData.define(DATA_HOSTILE, false);
        this.entityData.define(DATA_OWNER, Optional.empty());
    }

    // ------------------------------------------------------------- ai setup

    @Override
    protected void registerGoals() {
        // Goals are rebuilt whenever behaviour changes, see rebuildGoals().
        this.goalSelector.addGoal(0, new FloatGoal(this));
        applyBehaviourGoals();
    }

    private void rebuildGoals() {
        this.goalSelector.removeAllGoals(goal -> true);
        this.targetSelector.removeAllGoals(goal -> true);
        this.goalSelector.addGoal(0, new FloatGoal(this));
        this.goalSelector.addGoal(1, new OpenDoorGoal(this, true));
        applyBehaviourGoals();
    }

    private void applyBehaviourGoals() {
        switch (behaviour()) {
            case GUARD -> {
                this.goalSelector.addGoal(4, new LookAtPlayerGoal(this, Player.class, 8.0F));
                this.goalSelector.addGoal(5, new RandomLookAroundGoal(this));
            }
            case FOLLOW -> {
                this.goalSelector.addGoal(3, new FollowOwnerGoal(this, 1.15D, 4.0F, 2.0F));
                this.goalSelector.addGoal(6, new LookAtPlayerGoal(this, Player.class, 8.0F));
                this.goalSelector.addGoal(7, new RandomLookAroundGoal(this));
            }
            default -> {
                this.goalSelector.addGoal(4, new RandomStrollGoal(this, 0.85D));
                this.goalSelector.addGoal(6, new LookAtPlayerGoal(this, Player.class, 8.0F));
                this.goalSelector.addGoal(7, new RandomLookAroundGoal(this));
            }
        }
        if (isHostile()) {
            this.goalSelector.addGoal(2, new MeleeAttackGoal(this, 1.2D, false));
            this.targetSelector.addGoal(1, new HurtByTargetGoal(this));
            this.targetSelector.addGoal(2, new NearestAttackableTargetGoal<>(this, Player.class, true));
            this.targetSelector.addGoal(3, new NearestAttackableTargetGoal<>(this, Monster.class, true));
        }
    }

    // --------------------------------------------------------------- tick

    @Override
    public void tick() {
        super.tick();
        if (this.home == null) {
            this.home = this.blockPosition();
        }
    }

    @Override
    public boolean removeWhenFarAway(double distance) {
        return false; // NPCs are placed deliberately and must never despawn
    }

    @Override
    public boolean isPersistenceRequired() {
        return true;
    }

    // ------------------------------------------------------------- interact

    @Override
    public InteractionResult mobInteract(Player player, InteractionHand hand) {
        if (this.level().isClientSide) {
            // the menu opens on the client; the server has nothing to do yet
            return InteractionResult.SUCCESS;
        }
        if (player.isShiftKeyDown() && isOwnerOrOp(player)) {
            // shift-right-click with a name tag renames nothing: this is the
            // shortcut for opening the editor from the server side
            return InteractionResult.SUCCESS;
        }
        if (!dialogue().isEmpty()) {
            player.sendSystemMessage(Component.literal("<" + displayName() + "> " + dialogue()));
        } else {
            player.sendSystemMessage(Component.literal(displayName() + " has nothing to say."));
        }
        return InteractionResult.CONSUME;
    }

    public boolean isOwnerOrOp(Player player) {
        return player.hasPermissions(2) || getOwner().map(uuid -> uuid.equals(player.getUUID())).orElse(true);
    }

    // ------------------------------------------------------------- getters

    public String displayName() {
        String custom = this.entityData.get(DATA_NAME);
        if (custom == null || custom.isBlank()) {
            return "NPC";
        }
        return custom;
    }

    @Override
    public Component getName() {
        String custom = this.entityData.get(DATA_NAME);
        return custom == null || custom.isBlank()
                ? Component.literal("NPC")
                : Component.literal(custom);
    }

    @Override
    public Component getDisplayName() {
        return getName();
    }

    /** Sets the displayed name directly, bypassing the vanilla custom name. */
    public void setCustomNameDirect(String name) {
        this.entityData.set(DATA_NAME, name == null ? "" : name);
    }

    public String skin() {
        return this.entityData.get(DATA_SKIN);
    }

    public void setSkin(String skin) {
        this.entityData.set(DATA_SKIN, skin == null || skin.isBlank() ? "customnpcs:default" : skin);
    }

    public NpcBehaviour behaviour() {
        return NpcBehaviour.byOrdinal(this.entityData.get(DATA_BEHAVIOUR));
    }

    public void setBehaviour(NpcBehaviour behaviour) {
        this.entityData.set(DATA_BEHAVIOUR, behaviour.ordinal());
        if (!this.level().isClientSide) {
            rebuildGoals();
        }
    }

    public String dialogue() {
        String value = this.entityData.get(DATA_DIALOGUE);
        return value == null ? "" : value;
    }

    public void setDialogue(String dialogue) {
        this.entityData.set(DATA_DIALOGUE, dialogue == null ? "" : dialogue);
    }

    /** Scale in percent, 50..200. */
    public int scalePercent() {
        return this.entityData.get(DATA_SCALE);
    }

    public void setScalePercent(int percent) {
        this.entityData.set(DATA_SCALE, Math.max(50, Math.min(200, percent)));
    }

    public boolean isInvulnerableNpc() {
        return this.entityData.get(DATA_INVULNERABLE);
    }

    public void setInvulnerableNpc(boolean invulnerable) {
        this.entityData.set(DATA_INVULNERABLE, invulnerable);
    }

    public boolean isHostile() {
        return this.entityData.get(DATA_HOSTILE);
    }

    public void setHostile(boolean hostile) {
        this.entityData.set(DATA_HOSTILE, hostile);
        if (!this.level().isClientSide) {
            rebuildGoals();
        }
    }

    public Optional<UUID> getOwner() {
        return this.entityData.get(DATA_OWNER);
    }

    public void setOwner(@Nullable UUID owner) {
        this.entityData.set(DATA_OWNER, Optional.ofNullable(owner));
    }

    public BlockPos home() {
        return this.home == null ? this.blockPosition() : this.home;
    }

    public void setHome(BlockPos pos) {
        this.home = pos;
    }

    // --------------------------------------------------------------- damage

    @Override
    public boolean hurt(DamageSource source, float amount) {
        if (isInvulnerableNpc() && !(source.getEntity() instanceof Player)) {
            return false;
        }
        if (isInvulnerableNpc() && source.getEntity() instanceof Player player && !isOwnerOrOp(player)) {
            return false;
        }
        return super.hurt(source, amount);
    }

    @Override
    protected SoundEvent getHurtSound(DamageSource source) {
        return SoundEvents.PLAYER_HURT;
    }

    @Override
    protected SoundEvent getDeathSound() {
        return SoundEvents.PLAYER_DEATH;
    }

    @Override
    protected SoundEvent getAmbientSound() {
        return null; // NPCs are quiet by default
    }

    // ------------------------------------------------------------- movement

    /** Applies the configured scale to the hitbox. */
    @Override
    public EntityDimensions getDimensions(Pose pose) {
        return super.getDimensions(pose).scale(scalePercent() / 100.0F);
    }

    @Override
    public void onSyncedDataUpdated(EntityDataAccessor<?> key) {
        super.onSyncedDataUpdated(key);
        if (DATA_SCALE.equals(key)) {
            this.refreshDimensions();
        }
    }

    // ------------------------------------------------------------ nbt save

    @Override
    public void addAdditionalSaveData(CompoundTag tag) {
        super.addAdditionalSaveData(tag);
        tag.putString("NpcName", displayName());
        tag.putString("Skin", skin());
        tag.putInt("Behaviour", behaviour().ordinal());
        tag.putInt("Scale", scalePercent());
        tag.putString("Dialogue", dialogue());
        tag.putBoolean("InvulnerableNpc", isInvulnerableNpc());
        tag.putBoolean("Hostile", isHostile());
        getOwner().ifPresent(uuid -> tag.putUUID("Owner", uuid));
        if (this.home != null) {
            tag.putLong("Home", this.home.asLong());
        }
        ItemStack main = this.getItemBySlot(EquipmentSlot.MAINHAND);
        if (!main.isEmpty()) {
            tag.put("MainHand", main.save(new CompoundTag()));
        }
        ItemStack head = this.getItemBySlot(EquipmentSlot.HEAD);
        if (!head.isEmpty()) {
            tag.put("Head", head.save(new CompoundTag()));
        }
    }

    @Override
    public void readAdditionalSaveData(CompoundTag tag) {
        super.readAdditionalSaveData(tag);
        if (tag.contains("NpcName")) {
            this.entityData.set(DATA_NAME, tag.getString("NpcName"));
        }
        if (tag.contains("Skin")) {
            this.entityData.set(DATA_SKIN, tag.getString("Skin"));
        }
        if (tag.contains("Behaviour")) {
            this.entityData.set(DATA_BEHAVIOUR, tag.getInt("Behaviour"));
        }
        if (tag.contains("Scale")) {
            this.entityData.set(DATA_SCALE, tag.getInt("Scale"));
        }
        if (tag.contains("Dialogue")) {
            this.entityData.set(DATA_DIALOGUE, tag.getString("Dialogue"));
        }
        if (tag.contains("InvulnerableNpc")) {
            this.entityData.set(DATA_INVULNERABLE, tag.getBoolean("InvulnerableNpc"));
        }
        if (tag.contains("Hostile")) {
            this.entityData.set(DATA_HOSTILE, tag.getBoolean("Hostile"));
        }
        if (tag.hasUUID("Owner")) {
            this.entityData.set(DATA_OWNER, Optional.of(tag.getUUID("Owner")));
        }
        if (tag.contains("Home")) {
            this.home = BlockPos.of(tag.getLong("Home"));
        }
        if (tag.contains("MainHand")) {
            this.setItemSlot(EquipmentSlot.MAINHAND, ItemStack.of(tag.getCompound("MainHand")));
        }
        if (tag.contains("Head")) {
            this.setItemSlot(EquipmentSlot.HEAD, ItemStack.of(tag.getCompound("Head")));
        }
        if (!this.level().isClientSide) {
            rebuildGoals();
        }
    }

    // -------------------------------------------------------------- spawning

    @Nullable
    @Override
    public SpawnGroupData finalizeSpawn(ServerLevelAccessor level, DifficultyInstance difficulty,
                                        MobSpawnType reason, @Nullable SpawnGroupData spawnData,
                                        @Nullable CompoundTag tag) {
        this.setHome(this.blockPosition());
        if (behaviour() == NpcBehaviour.GUARD) {
            this.setItemSlot(EquipmentSlot.MAINHAND, new ItemStack(Items.IRON_SWORD));
        }
        return super.finalizeSpawn(level, difficulty, reason, spawnData, tag);
    }

    // ---------------------------------------------------------------- sounds

    @Override
    public boolean canBeLeashed(Player player) {
        return isOwnerOrOp(player);
    }

    // ------------------------------------------------------------- the goal

    /**
     * A follow goal small enough to keep in one file: walk toward the owner
     * while staying outside {@code stopDistance}, and teleport when left far
     * behind, which is what players expect from a companion.
     */
    static final class FollowOwnerGoal extends net.minecraft.world.entity.ai.goal.Goal {
        private final CustomNpc npc;
        private final double speed;
        private final float stopDistance;
        private final float startDistance;
        private LivingEntity owner;
        private int recalcCooldown;

        FollowOwnerGoal(CustomNpc npc, double speed, float startDistance, float stopDistance) {
            this.npc = npc;
            this.speed = speed;
            this.startDistance = startDistance;
            this.stopDistance = stopDistance;
            this.setFlags(java.util.EnumSet.of(Flag.MOVE, Flag.LOOK));
        }

        @Override
        public boolean canUse() {
            Optional<UUID> ownerId = this.npc.getOwner();
            if (ownerId.isEmpty() || this.npc.level().isClientSide) {
                return false;
            }
            Player player = this.npc.level().getPlayerByUUID(ownerId.get());
            if (player == null || player.isSpectator() || this.npc.distanceToSqr(player) < (double) (this.startDistance * this.startDistance)) {
                return false;
            }
            this.owner = player;
            return true;
        }

        @Override
        public boolean canContinueToUse() {
            return this.owner != null
                    && this.owner.isAlive()
                    && this.npc.distanceToSqr(this.owner) > (double) (this.stopDistance * this.stopDistance);
        }

        @Override
        public void stop() {
            this.owner = null;
            this.npc.getNavigation().stop();
        }

        @Override
        public void tick() {
            if (this.owner == null) {
                return;
            }
            this.npc.getLookControl().setLookAt(this.owner, 10.0F, (float) this.npc.getMaxHeadXRot());
            if (--this.recalcCooldown <= 0) {
                this.recalcCooldown = 10;
                if (this.npc.distanceToSqr(this.owner) > 256.0D && this.npc.getRandom().nextInt(12) == 0) {
                    // left far behind: catch up rather than walk the whole way
                    this.npc.teleportTo(this.owner.getX(), this.owner.getY(), this.owner.getZ());
                } else {
                    this.npc.getNavigation().moveTo(this.owner, this.speed);
                }
            }
        }
    }
}
