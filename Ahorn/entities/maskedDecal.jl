module SJ2021MaskedDecal

using ..Ahorn, Maple

@mapdef Entity "SJ2021/MaskedDecal" MaskedDecal(x::Integer, y::Integer, texture::String="", scaleX::Number=1.0, scaleY::Number=1.0)

const placements = Ahorn.PlacementDict(
    "Masked Decal (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        MaskedDecal
    )
)

function Ahorn.selection(entity::MaskedDecal)
    x, y = Ahorn.position(entity)
    sprite = "decals/$(get(entity, "texture", ""))"
    scaleX = get(entity, "scaleX", 1.0)
    scaleY = get(entity, "scaleY", 1.0)

    return Ahorn.getSpriteRectangle(sprite, x, y, sx=scaleX, sy=scaleY)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MaskedDecal, room::Maple.Room)
    sprite = "decals/$(get(entity, "texture", ""))"
    scaleX = get(entity, "scaleX", 1.0)
    scaleY = get(entity, "scaleY", 1.0)

    Ahorn.drawSprite(ctx, sprite, 0, 0, sx=scaleX, sy=scaleY)
end

function Ahorn.flipped(entity::MaskedDecal, horizontal::Bool)
    if horizontal
        entity.scaleX *= -1.0
    else
        entity.scaleY *= -1.0
    end
    return entity
end

animationRegex = r"\D+0*?$"
filterAnimations(s::String) = occursin(animationRegex, s)

function decalTextures()
    Ahorn.loadChangedExternalSprites!()
    textures = Ahorn.spritesToDecalTextures(Ahorn.getAtlas("Gameplay"))
    filter!(filterAnimations, textures)
    sort!(textures)
    return textures
end

Ahorn.editingOptions(entity::MaskedDecal) = Dict{String, Any}(
    "texture" => decalTextures()
)

end