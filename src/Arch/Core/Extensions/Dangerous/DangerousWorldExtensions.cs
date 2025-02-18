using Arch.LowLevel.Jagged;

namespace Arch.Core.Extensions.Dangerous;

// NOTE: I am omitting WorldExtensionsAttribute here, as these should be accessed through the original world instead of a wrapper world anyways.
/// <summary>
///     The <see cref="DangerousWorldExtensions"/> class
///     contains several <see cref="World"/> related extension methods which give acess to underlaying data structures that should only be modified when you exactly know what you are doing.
/// </summary>
public static class DangerousWorldExtensions
{
    /// <summary>
    ///     Sets the <see cref="World.Archetypes"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/> instance.</param>
    /// <param name="archetypes">The new list of <see cref="Archetype"/>s.</param>
    public static void SetArchetypes(this World world, List<Archetype> archetypes)
    {
        world.Archetypes.AddRange(archetypes);
        foreach (var archetype in archetypes)
        {
            world.Size += archetype.Entities;
            world.Capacity += archetype.EntitiesPerChunk * archetype.Size;
        }
    }

    /// <summary>
    ///     Ensures the capacity of the <see cref="World.EntityInfo"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/>.</param>
    /// <param name="capacity">The new capacity.</param>
    public static void EnsureCapacity(this World world, int capacity)
    {
        world.EntityInfo.EnsureCapacity(capacity);
    }

    /// <summary>
    ///     Sets the <see cref="EntityInfo.Archetype"/> for an <see cref="Entity"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/>.</param>
    /// <param name="entity">The <see cref="Entity"/>.</param>
    /// <param name="archetype">The <see cref="Archetype"/>.</param>
    public static void SetArchetype(this World world, Entity entity, Archetype archetype)
    {
        world.EntityInfo.EntitySlots[entity.Id].Archetype = archetype;
    }

    /// <summary>
    ///     Returns the <see cref="EntityInfoStorage.Versions"/> of a <see cref="World"/> for reading or modifiyng it.
    /// </summary>
    /// <param name="world">The <see cref="World"/> instance.</param>
    /// <returns>Its <see cref="EntityInfoStorage.Versions"/> array.</returns>
    public static JaggedArray<int> GetVersions(this World world)
    {
        return world.EntityInfo.Versions;
    }

    /// <summary>
    ///     Sets the <see cref="EntityInfoStorage.Versions"/> of a <see cref="World"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/> instance.</param>
    /// <param name="versions">The new versions array.</param>
    public static void SetVersions(this World world, JaggedArray<int> versions)
    {
        world.EntityInfo.Versions = versions;
    }

    /// <summary>
    ///     Returns the <see cref="EntityInfoStorage.EntitySlots"/> of a <see cref="World"/> for reading or modifiyng it.
    /// </summary>
    /// <param name="world">The <see cref="World"/> instance.</param>
    /// <returns>Its <see cref="EntityInfoStorage.EntitySlots"/> array.</returns>
    public static JaggedArray<(Archetype, (int,int))> GetSlots(this World world)
    {
        var array = world.EntityInfo.EntitySlots;
        return Unsafe.As<JaggedArray<(Archetype, (int,int))>>(array);
    }

    /// <summary>
    ///     Sets the <see cref="EntityInfoStorage.Slots"/> of a <see cref="World"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/> instance.</param>
    /// <param name="slots">The new slots array.</param>
    public static void SetSlots(this World world, JaggedArray<(Archetype, (int,int))> slots)
    {
        world.EntityInfo.EntitySlots = Unsafe.As<JaggedArray<EntitySlot>>(slots);
    }

    /// <summary>
    ///     Returns the <see cref="Slot"/> of an <see cref="Entity"/>.
    /// </summary>
    /// <param name="world">The <see cref="World"/>.</param>
    /// <param name="entity">The <see cref="Entity"/>.</param>
    /// <returns>The <see cref="Slot"/> as a <see cref="ValueTuple{T,TT}"/>.</returns>
    public static (int, int) GetSlot(this World world, Entity entity)
    {
        ref var slot = ref world.EntityInfo.GetSlot(entity.Id);
        return (slot.Index, slot.ChunkIndex);
    }
}
