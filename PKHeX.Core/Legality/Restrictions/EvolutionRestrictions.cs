using System;
using static PKHeX.Core.Move;
using static PKHeX.Core.Species;

namespace PKHeX.Core;

/// <summary>
/// Restriction logic for evolutions that are a little more complex than <see cref="EvolutionMethod"/> can simply check.
/// </summary>
/// <remarks>
/// Currently only checks "is able to know a move required to level up".
/// </remarks>
internal static class EvolutionRestrictions
{
    /// <summary>
    /// List of species that evolve from a previous species having a move while leveling up
    /// </summary>
    private static ushort GetSpeciesEvolutionMove(ushort species) => species switch
    {
        (int)Eevee => EEVEE,
        (int)MimeJr => (int)Mimic,
        (int)Bonsly => (int)Mimic,
        (int)Aipom => (int)DoubleHit,
        (int)Lickitung => (int)Rollout,
        (int)Tangela => (int)AncientPower,
        (int)Yanma => (int)AncientPower,
        (int)Piloswine => (int)AncientPower,
        (int)Steenee => (int)Stomp,
        (int)Clobbopus => (int)Taunt,
        (int)Stantler => (int)PsyshieldBash,
        (int)Qwilfish => (int)BarbBarrage,
        (int)Primeape => (int)RageFist,
        (int)Girafarig => (int)TwinBeam,
        (int)Dunsparce => (int)HyperDrill,
        _ => NONE,
    };

    private const ushort NONE = 0;
    private const ushort EEVEE = ushort.MaxValue;

    private static ReadOnlySpan<ushort> EeveeFairyMoves =>
    [
        (int)Charm,
        (int)BabyDollEyes,
    ];

    /// <summary>
    /// Checks if the <see cref="pk"/> is correctly evolved, assuming it had a known move requirement evolution in its evolution chain.
    /// </summary>
    /// <returns>True if unnecessary to check or the evolution was valid.</returns>
    public static bool IsValidEvolutionWithMove(PKM pk, LegalInfo info)
    {
        // Known-move evolutions were introduced in Gen4.
        if (pk.Format < 4) // doesn't exist yet!
            return true;

        // OK if un-evolved from original encounter
        var enc = info.EncounterOriginal;
        ushort species = pk.Species;
        if (enc.Species == species)
            return true;

        // Exclude evolution paths that did not require a move w/level-up evolution
        var move = GetSpeciesEvolutionMove(enc.Species);
        if (move is NONE)
            return true; // not a move evolution
        if (move is EEVEE)
            return species != (int)Sylveon || IsValidEvolutionWithMoveSylveon(pk, enc, info);
        if (!IsMoveSlotAvailable(info.Moves))
            return false;

        if (pk.HasMove(move))
            return true;

        var head = LearnGroupUtil.GetCurrentGroup(pk);
        return MemoryPermissions.GetCanKnowMove(enc, move, info.EvoChainsAllGens, pk, head);
    }

    private static bool IsValidEvolutionWithMoveSylveon(PKM pk, IEncounterTemplate enc, LegalInfo info)
    {
        if (!IsMoveSlotAvailable(info.Moves))
            return false;

        foreach (var move in EeveeFairyMoves)
        {
            if (pk.HasMove(move))
                return true;
        }

        var head = LearnGroupUtil.GetCurrentGroup(pk);
        foreach (var move in EeveeFairyMoves)
        {
            if (MemoryPermissions.GetCanKnowMove(enc, move, info.EvoChainsAllGens, pk, head))
                return true;
        }
        return false;
    }

    private static bool IsMoveSlotAvailable(ReadOnlySpan<MoveResult> moves)
    {
        // If the Pokémon does not currently have the move, it could have been an egg move that was forgotten.
        // This requires the Pokémon to not have 4 other moves identified as egg moves or inherited level up moves.
        // If any move is not an egg source, then a slot could have been forgotten.
        foreach (var move in moves)
        {
            if (!move.Info.Method.IsEggSource())
                return true;
        }
        return false;
    }
}
