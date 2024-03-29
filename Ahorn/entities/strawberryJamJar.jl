module SJ2021StrawberryJamJar

using ..Ahorn, Maple

@mapdef Entity "SJ2021/StrawberryJamJar" StrawberryJamJar(x::Integer, y::Integer, map::String="Celeste/1-ForsakenCity", sprite::String="trailer",
    returnToLobbyMode::String="SetReturnToHere", allowSaving::Bool=true)

const placements = Ahorn.PlacementDict(
    "Strawberry Jam Jar (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        StrawberryJamJar
    )
)

Ahorn.editingOptions(entity::StrawberryJamJar) = Dict{String, Any}(
    "sprite" => String["trailer", "beginner", "intermediate", "advanced", "expert", "grandmaster"],
    "returnToLobbyMode" => String["SetReturnToHere", "RemoveReturn", "DoNotChangeReturn"]
)

function Ahorn.selection(entity::StrawberryJamJar)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/jamJar/$(entity.sprite)/jarfull_idle00", x, y, jx=0.5, jy=1)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::StrawberryJamJar)
    Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/jamJar/$(entity.sprite)/jarfull_idle00", 0, 0, jx=0.5, jy=1)
end

end
