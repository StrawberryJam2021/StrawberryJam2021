module SJ2021_AntiGravJelly
using ..Ahorn, Maple

@mapdef Entity "SJ2021/AntiGravJelly" AntiGravJelly(x::Integer, y::Integer,
   bubble::Bool=false, downThrowMultiplier::Number=1.0, diagThrowXMultiplier::Number=1.0,
   diagThrowYMultiplier::Number=1.0, gravity::Number=-30.0)

const placements = Ahorn.PlacementDict(
   "Antigravity Jelly (SJ2021)" => Ahorn.EntityPlacement(
    AntiGravJelly,
    "rectangle"
   )
)

sprite = "objects/glider/idle0"

function Ahorn.selection(entity::AntiGravJelly)
   x, y = Ahorn.position(entity)

   return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AntiGravJelly, room::Maple.Room)
        Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end