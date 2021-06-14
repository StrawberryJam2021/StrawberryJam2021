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
)

const sprite = "objects/StrawberryJam2021/wonkyCassetteBlockController/icon"

function Ahorn.selection(entity::WonkyCassetteBlockController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WonkyCassetteBlockController)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end