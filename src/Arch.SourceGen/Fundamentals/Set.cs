namespace Arch.SourceGen;

public static class SetExtensions
{
    public static StringBuilder AppendChunkIndexSets(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendChunkIndexSet(index);
        }

        return sb;
    }

    public static StringBuilder AppendChunkIndexSet(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInParams(amount);
        var arrays = new StringBuilder().GetChunkArrays(amount);

        var sets = new StringBuilder();
        for (var index = 0; index <= amount; index++)
        {
            sets.AppendLine($"t{index}Array[index] = t{index}Component;");
        }

        var template =
            $$"""
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set<{{generics}}>(in int index, {{parameters}})
            {
                {{arrays}}
                {{sets}}
            }
            """;

        return sb.AppendLine(template);
    }

    public static StringBuilder AppendArchetypeSets(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendArchetypeSet(index);
        }

        return sb;
    }

    public static StringBuilder AppendArchetypeSet(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInParams(amount);
        var insertParameters = new StringBuilder().InsertGenericInParams(amount);

        var template =
            $$"""
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Set<{{generics}}>(ref Slot slot, {{parameters}})
            {
                ref var chunk = ref GetChunk(slot.ChunkIndex);
                chunk.Set<{{generics}}>(slot.Index, {{insertParameters}});
            }
            """;

        return sb.AppendLine(template);
    }

    public static StringBuilder AppendArchetypeSetRanges(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendArchetypeSetRange(index);
        }

        return sb;
    }

    public static StringBuilder AppendArchetypeSetRange(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInDefaultParams(amount,"ComponentValue");
        var getFirstElements = new StringBuilder().GetFirstGenericElements(amount);
        var getComponents = new StringBuilder().GetGenericComponents(amount);

        var assignComponents = new StringBuilder();
        for (var index = 0; index <= amount; index++)
        {
            assignComponents.AppendLine($"t{index}Component = t{index}ComponentValue;");
        }

        var template =
            $$"""
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void SetRange<{{generics}}>(in Slot from, in Slot to, {{parameters}})
            {
                // Set the added component, start from the last slot and move down
                for (var chunkIndex = from.ChunkIndex; chunkIndex >= to.ChunkIndex; --chunkIndex)
                {
                    ref var chunk = ref GetChunk(in chunkIndex);
                    {{getFirstElements}}
                    foreach(var entityIndex in chunk)
                    {
                        {{getComponents}}
                        {{assignComponents}}

                        // Break to prevent old entities receiving the new value.
                        if (chunkIndex == to.ChunkIndex && entityIndex == to.Index)
                        {
                            break;
                        }
                    }
                }
            }
            """;

        return sb.AppendLine(template);
    }

    public static StringBuilder AppendWorldSets(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendWorldSet(index);
        }

        return sb;
    }

    public static StringBuilder AppendWorldSet(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInParams(amount);
        var insertParams = new StringBuilder().InsertGenericInParams(amount);

        var template =
            $$"""
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set<{{generics}}>(in Entity entity, {{parameters}})
            {
                var entityInfo = EntityInfo[entity.Id];
                var archetype = entityInfo.Archetype;
                archetype.Set<{{generics}}>(ref entityInfo.Slot, {{insertParams}});
            }
            """;

        return sb.AppendLine(template);
    }

    public static StringBuilder AppendEntitySets(this StringBuilder sb, int amount)
    {
        for (var index = 1; index < amount; index++)
        {
            sb.AppendEntitySet(index);
        }

        return sb;
    }

    public static StringBuilder AppendEntitySet(this StringBuilder sb, int amount)
    {
        var generics = new StringBuilder().GenericWithoutBrackets(amount);
        var parameters = new StringBuilder().GenericInParams(amount);
        var insertParams = new StringBuilder().InsertGenericInParams(amount);

        var template =
            $$"""
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Set<{{generics}}>(this in Entity entity, {{parameters}})
            {
                var world = World.Worlds[entity.WorldId];
                world.Set<{{generics}}>(in entity, {{insertParams}});
            }
            """;

        return sb.AppendLine(template);
    }
}
