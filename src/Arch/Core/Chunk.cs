using System.Diagnostics.Contracts;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using CommunityToolkit.HighPerformance;

namespace Arch.Core;

public readonly unsafe struct UnsafeArray<T> : IDisposable where T : unmanaged
{

    internal readonly T* _ptr;

    public UnsafeArray(int count)
    {
#if NET6_0_OR_GREATER
        _ptr = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * count));
#else
        _ptr = (T*)Marshal.AllocHGlobal(sizeof(T) * count);
#endif
        Count = count;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    public readonly int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Count;
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _ptr[i];
    }

    public void Dispose()
    {
#if NET6_0_OR_GREATER
        NativeMemory.Free(_ptr);
#else
        Marshal.FreeHGlobal((IntPtr)_ptr);
#endif
    }

    /// <summary>
    ///     Converts an <see cref="UnsafeArray{T}"/> into a void pointer.
    /// </summary>
    /// <param name="instance">The <see cref="UnsafeArray{T}"/> instance.</param>
    /// <returns>A void pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator void*(UnsafeArray<T> instance)
    {
        return (void*)instance._ptr;
    }
}

public unsafe struct UnsafeArray
{
    /// <summary>
    ///  Copies the a part of the <see cref="UnsafeArray{T}"/> to the another <see cref="UnsafeArray{T}"/>.
    /// </summary>
    /// <param name="source">The source <see cref="UnsafeArray{T}"/>.</param>
    /// <param name="index">The start index in the source <see cref="UnsafeArray{T}"/>.</param>
    /// <param name="destination">The destination <see cref="UnsafeArray{T}"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="UnsafeArray{T}"/>.</param>
    /// <param name="length">The length indicating the amount of items being copied.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal static void Copy<T>(ref UnsafeArray<T> source, int index, ref UnsafeArray<T> destination, int destinationIndex, int length) where T : unmanaged
    {
        var size = sizeof(T);
        var bytes = size * length;
        var sourcePtr = (void*)(source._ptr + (size*index));
        var destinationPtr = (void*)(destination._ptr + (size*destinationIndex));
        Buffer.MemoryCopy(sourcePtr, destinationPtr, bytes, bytes);
    }


    /// <summary>
    ///     Fills an <see cref="UnsafeArray{T}"/> with a given value.
    /// </summary>
    /// <param name="source">The <see cref="UnsafeArray{T}"/> instance.</param>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal static void Fill<T>(ref UnsafeArray<T> source, in T value = default) where T : unmanaged
    {
        for (int index = 0; index < source.Count; index++)
        {
            source[index] = value;
        }
    }

}

/// <summary>
///     The <see cref="ComponentArray"/> struct
///     represents an hybrid array that either wraps an <see cref="Array"/> or an <see cref="NativeArray"/>.
///     It mirrors the most important array operations to acess the underlaying array regardless of its implementation.
/// </summary>
public readonly unsafe struct ComponentArray : IDisposable
{

    /// <summary>
    ///     An <see cref="IntPtr"/> pointing to native memory that represents the array.
    /// </summary>
    private readonly IntPtr NativeArray;

    /// <summary>
    ///     An gc managed <see cref="Array"/> that represents the array.
    /// </summary>
    private readonly Array Array;


    /// <summary>
    ///     Initializes a new instance of the <see cref="ComponentArray"/> struct.
    ///     Accepts a <see cref="IntPtr"/> and creates an unmanaged <see cref="ComponentArray"/> instance.
    /// </summary>
    /// <param name="nativeArray">The <see cref="IntPtr"/> pointing towards the native allocated memory block indicating an array.</param>
    /// <param name="componentType">The <see cref="ComponentType"/> stored.</param>
    /// <param name="capacity">The capacity.</param>
    internal ComponentArray(IntPtr nativeArray, ComponentType componentType, int capacity)
    {
        ComponentType = componentType;
        NativeArray = nativeArray;
        IsManaged = false;
        Capacity = capacity;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ComponentArray"/> struct.
    ///     Accepts a <see cref="Array"/> and creates an managed <see cref="ComponentArray"/> instance.
    /// </summary>
    /// <param name="array">The <see cref="Array"/>.</param>
    /// <param name="componentType">The <see cref="ComponentType"/> stored.</param>
    /// <param name="capacity">The capacity.</param>
    internal ComponentArray(Array array, ComponentType componentType, int capacity)
    {
        ComponentType = componentType;
        Array = array;
        IsManaged = true;
        Capacity = capacity;
    }

    /// <summary>
    ///     The <see cref="ComponentType"/> that is stored by this array.
    /// </summary>
    public readonly ComponentType ComponentType { get; }

    /// <summary>
    ///     True if the underlaying array is a managed <see cref="Array"/> and no native memory <see cref="NativeArray"/>.
    /// </summary>
    public readonly bool IsManaged { get; }

    /// <summary>
    ///     The arrays allocated capacity.
    /// </summary>
    public readonly int Capacity { get; }

    /// <summary>
    ///     Sets an item at an index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="value">The item value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(in int index, object value)
    {
        if (!IsManaged)
        {
            var ptr = NativeArray + (ComponentType.ByteSize * index);
            Marshal.StructureToPtr(value, ptr, false);
        }
        else
        {
            Array.SetValue(value, index);
        }
    }

    /// <summary>
    ///     Returns an item from an index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The item casted to an <see cref="object"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Get(in int index)
    {
        if (Array == null)
        {
            var ptr = NativeArray + (ComponentType.ByteSize * index);
            return Marshal.PtrToStructure(ptr, ComponentType.Type);
        }

        return Array.GetValue(index);
    }

    /// <summary>
    ///     Disposes this <see cref="ComponentArray"/> instance and releases memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (IsManaged)
        {
            return;
        }

        Marshal.FreeHGlobal(NativeArray);
    }

    /// <summary>
    ///     Converts this <see cref="ComponentArray"/> instance into its <see cref="Span{T}"/> representation.
    /// </summary>
    /// <typeparam name="T">The generic type of this underlaying array.</typeparam>
    /// <returns>A new instance of an <see cref="Span{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan<T>()
    {
        // Handle object components.
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var arrayRef = Unsafe.As<T[]>(Array);
            return new Span<T>(arrayRef);
        }

        return new Span<T>((void*)NativeArray, Capacity);
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="ComponentArray"/> struct.
    ///     Determines if the passed <see cref="ComponentType"/> is managed and therefore will either allocate a native <see cref="ComponentArray"/> or a gc one.
    /// </summary>
    /// <param name="type">The <see cref="ComponentType"/> that the <see cref="ComponentArray"/> should store.</param>
    /// <param name="Capacity">The capacity.</param>
    /// <returns>A new <see cref="ComponentArray"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ComponentArray CreateInstance(ComponentType type, int Capacity)
    {
        if (!type.IsManaged)
        {
            var ptr = Marshal.AllocHGlobal(type.ByteSize * Capacity);
            return new ComponentArray(ptr, type, Capacity);
        }

        var array = Array.CreateInstance(type, Capacity);
        return new ComponentArray(array, type, Capacity);
    }

    /// <summary>
    ///  Copies the whole <see cref="Chunk"/> (with all its entities and components) or a part from it to the another <see cref="Chunk"/>.
    /// </summary>
    /// <param name="source">The source <see cref="Chunk"/>.</param>
    /// <param name="index">The start index in the source <see cref="Chunk"/>.</param>
    /// <param name="destination">The destination <see cref="Chunk"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="Chunk"/>.</param>
    /// <param name="length">The length indicating the amount of <see cref="Entity"/>s being copied.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal static void Copy(ref ComponentArray source, int index, ref ComponentArray destination, int destinationIndex, int length)
    {
        // Can not copy since im lazy
        if (source.IsManaged != destination.IsManaged || source.ComponentType != destination.ComponentType)
        {
            return;
        }

        // Copy content
        if (!source.IsManaged)
        {
            var bytes = source.ComponentType.ByteSize * length;
            var sourcePtr = (void*)(source.NativeArray + (source.ComponentType.ByteSize*index));
            var destinationPtr = (void*)(destination.NativeArray + (source.ComponentType.ByteSize*destinationIndex));
            Buffer.MemoryCopy(sourcePtr, destinationPtr, bytes, bytes);
        }
        else
        {
            Array.Copy(source.Array, index, destination.Array, destinationIndex, length);
        }
    }

    /// <summary>
    ///     Converts an <see cref="ComponentArray"/> into a void pointer.
    /// </summary>
    /// <param name="instance">The <see cref="ComponentArray"/> instance.</param>
    /// <returns>A void pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator void*(ComponentArray instance) => (void*)instance.NativeArray;
}

/// <summary>
///     The <see cref="Chunk"/> struct represents a contiguous block of memory in which various components are stored in Structure of Arrays.
///     Chunks are internally allocated and filled by <see cref="Archetype"/>'s.
///     Through them it is possible to efficiently provide or trim memory for additional entities.
/// </summary>
[SkipLocalsInit]  // Really a speed improvements? The benchmark only showed a slight improvement
public unsafe partial struct Chunk : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Chunk"/> struct.
    ///     Automatically creates a lookup array for quick access to internal components.
    /// </summary>
    /// <param name="capacity">How many entities of the respective component structure fit into this <see cref="Chunk"/>.</param>
    /// <param name="types">The respective component structure of all entities in this <see cref="Chunk"/>.</param>
    internal Chunk(int capacity, params ComponentType[] types)
        : this(capacity, types.ToLookupArray(), types) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Chunk"/> struct
    /// </summary>
    /// <param name="capacity">How many entities of the respective component structure fit into this <see cref="Chunk"/>.</param>
    /// <param name="componentIdToArrayIndex">A lookup array which maps the component id to the array index of the component array.</param>
    /// <param name="types">The respective component structure of all entities in this <see cref="Chunk"/>.</param>
    internal Chunk(int capacity, UnsafeArray<int> componentIdToArrayIndex, params ComponentType[] types)
    {
        // Calculate capacity and init arrays.
        Size = 0;
        Capacity = capacity;

        Entities = (Entity*)Marshal.AllocHGlobal(sizeof(Entity) * Capacity);
        Components = new ComponentArray[types.Length];

        // Allocate arrays for types.
        ComponentIdToArrayIndex = componentIdToArrayIndex;
        for (var index = 0; index < types.Length; index++)
        {
            var type = types[index];
            Components[index] = ComponentArray.CreateInstance(type, Capacity);
        }
    }


    /// <summary>
    ///     The <see cref="Arch.Core.Entity"/>'s that are stored in this chunk.
    ///     Can be accessed during the iteration.
    /// </summary>
    public readonly Entity* Entities { [Pure] [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

    /// <summary>
    ///     The component arrays in which the components of the <see cref="Arch.Core.Entity"/>'s are stored.
    ///     Represent the component structure.
    ///     They can be accessed quickly using the <see cref="ComponentIdToArrayIndex"/> or one of the chunk methods.
    /// </summary>
    public readonly ComponentArray[] Components { [Pure] [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

    /// <summary>
    ///     The lookup array that maps component ids to component array indexes to quickly access them.
    /// </summary>
    public readonly UnsafeArray<int> ComponentIdToArrayIndex { [Pure] [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

    /// <summary>
    ///     The number of occupied <see cref="Arch.Core.Entity"/> slots in this <see cref="Chunk"/>.
    /// </summary>
    public int Size { [Pure] [MethodImpl(MethodImplOptions.AggressiveInlining)] get; [MethodImpl(MethodImplOptions.AggressiveInlining)] internal set; }

    /// <summary>
    ///     The number of possible <see cref="Arch.Core.Entity"/>'s in this <see cref="Chunk"/>.
    /// </summary>
    public int Capacity { [Pure] [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

    /// <summary>
    ///     Inserts an entity into the <see cref="Chunk"/>.
    /// </summary>
    /// <param name="entity">The <see cref="Arch.Core.Entity"/> that will be inserted.</param>
    /// <returns>The index occupied by the <see cref="Arch.Core.Entity"/> in the chunk.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Add(Entity entity)
    {
        Entities[Size] = entity;
        Size++;

        return Size - 1;
    }

    /// <summary>
    ///     Sets or replaces a component for an index in the chunk.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index in the array.</param>
    /// <param name="cmp">The component value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(int index, in T cmp)
    {
        var array = GetSpan<T>();
        array[index] = cmp;
    }

    /// <summary>
    ///     Checks if a component is included in this <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>True if included, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public bool Has<T>()
    {
        var id = Component<T>.ComponentType.Id;
        return id < ComponentIdToArrayIndex.Count && ComponentIdToArrayIndex[id] != 1;
    }

    /// <summary>
    ///     Returns a component from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public ref T Get<T>(int index)
    {
        var array = GetSpan<T>();
        return ref array[index];
    }

    /// <summary>
    ///     Returns a component at the index of the passed array.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="first">The first element of the array.</param>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public ref T Get<T>(ref T first, int index)
    {
        return ref Unsafe.Add(ref first, index);
    }

    /// <summary>
    ///     Returns a component and <see cref="Arch.Core.Entity"/> from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public EntityComponents<T> GetRow<T>(int index)
    {
        var array = GetSpan<T>();
        return new EntityComponents<T>(ref Entities[index], ref array[index]);
    }

    /// <summary>
    ///     Returns an <see cref="Arch.Core.Entity"/> at the index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>A reference to the <see cref="Arch.Core.Entity"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public ref Entity Entity(int index)
    {
        return ref Entities[index];
    }

    /// <summary>
    ///     Removes the <see cref="Arch.Core.Entity"/> at an index with all its components.
    ///     Copies the last <see cref="Arch.Core.Entity"/> in its place to ensure a uniform array.
    /// </summary>
    /// <param name="index">Its index.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Remove(int index)
    {
        // Last entity in archetype.
        var lastIndex = Size - 1;

        // Copy last entity to replace the removed one.
        Entities[index] = Entities[lastIndex];
        for (var i = 0; i < Components.Length; i++)
        {
            // Either copy native memory, or the managed elements
            ref var array = ref Components[i];
            ComponentArray.Copy(ref array, lastIndex, ref array, index, 1);
        }

        // Update the mapping.
        Size--;
    }

    /// <summary>
    ///     Creates and returns a new <see cref="EntityEnumerator"/> instance to iterate over all used rows representing <see cref="Arch.Core.Entity"/>'s.
    /// </summary>
    /// <returns>A new <see cref="EntityEnumerator"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityEnumerator GetEnumerator()
    {
        return new EntityEnumerator(Size);
    }

    /// <summary>
    ///     Cleares this <see cref="Chunk"/>, an efficient method to delete all <see cref="Arch.Core.Entity"/>s.
    ///     Does not dispose any resources nor modifies its <see cref="Capacity"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Size = 0;
    }

    /// <summary>
    ///     Disposes this <see cref="Chunk"/> and all underlaying allocated arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        for (var index = 0; index < Components.Length; index++)
        {
            ref var components = ref Components[index];
            components.Dispose();
        }
    }

    /// <summary>
    ///     Converts this <see cref="Chunk"/> to a human readable string.
    /// </summary>
    /// <returns>A string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return $"Chunk = {{ {nameof(Capacity)} = {Capacity}, {nameof(Size)} = {Size} }}";
    }
}

public partial struct Chunk
{

    /// <summary>
    ///     Returns the component array index of a component.
    /// </summary>
    /// <typeparam name="T">The componen type.</typeparam>
    /// <returns>The index in the <see cref="Components"/> array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    private int Index<T>()
    {
        var id = Component<T>.ComponentType.Id;
        return ComponentIdToArrayIndex[id];
    }

    /// <summary>
    ///     Returns the component array <see cref="Span{T}"/> for a given component in an unsafe manner.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The array <see cref="Span{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public Span<T> GetSpan<T>()
    {
        var index = Index<T>();
        Debug.Assert(index != -1, "Index is out of bounds");
        ref var array = ref Components[index];
        return array.AsSpan<T>();
    }

    /// <summary>
    ///     Returns a reference to the first element of a component from its component array in an unsafe manner.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>A reference to the first element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public ref T GetFirst<T>()
    {
        return ref GetSpan<T>()[0];
    }
}

public partial struct Chunk
{

    /// <summary>
    ///     Sets or replaces a component for an index in the chunk.
    /// </summary>
    /// <param name="index">The index in the array.</param>
    /// <param name="cmp">The component value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, object cmp)
    {
        var componentArray = GetComponentArray(cmp.GetType());
        componentArray.Set(in index, cmp);
    }

    /// <summary>
    ///     Checks if a component is included in this <see cref="Chunk"/>.
    /// </summary>
    /// <param name="t">The type.</param>
    /// <returns>True if included, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public bool Has(ComponentType t)
    {
        var id = Component.GetComponentType(t).Id;
        if (id >= ComponentIdToArrayIndex.Count)
        {
            return false;
        }

        return ComponentIdToArrayIndex[id] != -1;
    }

    /// <summary>
    ///     Returns a component from an index within the <see cref="Chunk"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="index">The index.</param>
    /// <returns>A component casted to an <see cref="object"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public object Get(int index, ComponentType type)
    {
        var array = GetComponentArray(type);
        return array.Get(in index);
    }

    /// <summary>
    ///     Returns the component array index of a component by its type.
    /// </summary>
    /// <param name="type">The <see cref="ComponentType"/>.</param>
    /// <returns>The index in the <see cref="Components"/> array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    private int Index(ComponentType type)
    {
        var id = type.Id;
        if (id >= ComponentIdToArrayIndex.Count)
        {
            return -1;
        }

        return ComponentIdToArrayIndex[id];
    }

    /// <summary>
    ///      Returns the <see cref="ComponentArray"/> for a given component type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The <see cref="Array"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public ref ComponentArray GetComponentArray(ComponentType type)
    {
        var index = Index(type);
        return ref Components[index];
    }
}

public partial struct Chunk
{

    /// <summary>
    ///  Copies the whole <see cref="Chunk"/> (with all its entities and components) or a part from it to the another <see cref="Chunk"/>.
    /// </summary>
    /// <param name="source">The source <see cref="Chunk"/>.</param>
    /// <param name="index">The start index in the source <see cref="Chunk"/>.</param>
    /// <param name="destination">The destination <see cref="Chunk"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="Chunk"/>.</param>
    /// <param name="length">The length indicating the amount of <see cref="Entity"/>s being copied.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal unsafe static void Copy(ref Chunk source, int index, ref Chunk destination, int destinationIndex, int length)
    {
        // Copy entities array
        var bytes = sizeof(Entity) * length;
        Buffer.MemoryCopy(source.Entities + index, destination.Entities + destinationIndex, bytes, bytes );
        CopyComponents(ref source, index, ref destination, destinationIndex, length);
    }

    /// <summary>
    ///     Copies an <see cref="Arch.Core.Entity"/> components at one index to another <see cref="Chunk"/>-index.
    /// </summary>
    /// <param name="source">The source <see cref="Chunk"/>.</param>
    /// <param name="index">The start index in the source <see cref="Chunk"/>.</param>
    /// <param name="destination">The destination <see cref="Chunk"/>.</param>
    /// <param name="destinationIndex">The start index in the destination <see cref="Chunk"/>.</param>
    /// <param name="length">The length indicating the amount of <see cref="Entity"/>s being copied.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal static void CopyComponents(ref Chunk source, int index, ref Chunk destination, int destinationIndex, int length)
    {
        // Arrays
        var sourceComponents = source.Components;

        // Copy component arrays
        for (var i = 0; i < sourceComponents.Length; i++)
        {
            ref var sourceArray = ref sourceComponents[i];
            if (!destination.Has(sourceArray.ComponentType))
            {
                continue;
            }

            var destinationArray = destination.GetComponentArray(sourceArray.ComponentType);
            ComponentArray.Copy(ref sourceArray, index, ref destinationArray, destinationIndex, length);
        }
    }

    /// <summary>
    ///     Transfers the last <see cref="Arch.Core.Entity"/> of the referenced <see cref="Chunk"/> into this <see cref="Chunk"/> at the given index.
    /// </summary>
    /// <param name="index">The index of the <see cref="Arch.Core.Entity"/>.</param>
    /// <param name="chunk">The <see cref="Chunk"/> we want transfer the last <see cref="Arch.Core.Entity"/> from.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    internal unsafe int Transfer(int index, ref Chunk chunk)
    {
        // Get last entity
        var lastIndex = chunk.Size - 1;
        var lastEntity = chunk.Entity(lastIndex);

        // Replace index entity with the last entity from the other chunk
        Entities[index] = lastEntity;
        for (var i = 0; i < Components.Length; i++)
        {
            ref var sourceArray = ref chunk.Components[i];
            ref var desArray = ref Components[i];
            ComponentArray.Copy(ref sourceArray, lastIndex, ref desArray, index, 1);
        }

        chunk.Size--;
        return lastEntity.Id;
    }
}
