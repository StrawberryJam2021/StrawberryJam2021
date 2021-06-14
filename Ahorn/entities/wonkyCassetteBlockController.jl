module SJ2021WonkyCassetteBlockController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WonkyCassetteBlockController" WonkyCassetteBlockController(
    x::Integer,
    y::Integer,
    bpm::Integer=90,
    bars::Integer=16,
    timeSignature::String="4/4",
    sixteenthNoteParam="sixteenth_note",
)

const placements = Ahorn.PlacementDict(
    "Wonky Cassette Block Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WonkyCassetteBlockController,
    ),
    "Wonky Cassette Block Controller\n(116 BPM, 16 bars of 7/8, \"78_eighth_note\") (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WonkyCassetteBlockController,
        "point",
        Dict{String, Any}(
            "bpm" => 116,
            "bars" => 16,
            "timeSignature" => "7/8",
            "sixteenthNoteParam" => "78_eighth_note",
        )
    ),
    "Wonky Cassette Block Controller\n(116 BPM, 6 bars of 10/8, \"108_eighth_note\") (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WonkyCassetteBlockController,
        "point",
        Dict{String, Any}(
            "bpm" => 116,
            "bars" => 6,
            "timeSignature" => "10/8",
            "sixteenthNoteParam" => "108_eighth_note",
        )
    ),
    "Wonky Cassette Block Controller\n(174 BPM, 8 bars of 13/8, \"138_eighth_note\") (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WonkyCassetteBlockController,
        "point",
        Dict{String, Any}(
            "bpm" => 174,
            "bars" => 8,
            "timeSignature" => "13/8",
            "sixteenthNoteParam" => "138_eighth_note",
        )
    ),
)

const sprite = "objects/StrawberryJam2021/wonkyCassetteBlockController/icon"

function Ahorn.selection(entity::WonkyCassetteBlockController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WonkyCassetteBlockController)
    Ahorn.drawSprite(ctx, sprite, 0, 0)

    timeSignature = String(get(entity.data, "timeSignature", "4/4"))
    values = split(replace(timeSignature, r"\s" => ""), "/")

    timeSignatureNumerator = "4"
    timeSignatureDenominator = "4"
    if length(values) == 2
        timeSignatureNumerator = (length(values[1]) <= 2 && length(values[1]) > 0) ? values[1] : "4"
        timeSignatureDenominator = (length(values[2]) <= 2 && length(values[2])) > 0 ? values[2] : "4"
    end
    color = (1.0, 1.0, 1.0, 1.0)
    Ahorn.drawCenteredText(ctx, String(timeSignatureNumerator), -8, -5, 8, 5, tint=color)
    Ahorn.drawCenteredText(ctx, String(timeSignatureDenominator), -8, 1, 8, 5, tint=color)
end

end