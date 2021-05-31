module SJ2021HorizontalTempleGate
using ..Ahorn, Maple
@mapdef Entity "SJ2021/HorizontalTempleGate" HorizontalTempleGate(x::Integer, y::Integer, flag::String="", texture::String="objects/StrawberryJam2021/horizontalTempleGate/default", inverted::Bool=false, direction::String="left", type::String="flagActive", openWidth::Integer=0)

const placements = Ahorn.PlacementDict(
   "Horizontal Temple Gate (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
      HorizontalTempleGate
   )
)

Ahorn.editingOptions(entity::HorizontalTempleGate) = Dict{String, Any}(
  "direction" => [
	"left",
	"right",
	"center"
  ],
  "type" => [
	"nearestSwitch",
	"touchSwitches",
	"flagActive"
  ]
)

function Ahorn.selection(entity::HorizontalTempleGate)
   x, y = Ahorn.position(entity)
   width = 48
   height = 15

   return Ahorn.Rectangle(x, y-3, width, height)

end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::HorizontalTempleGate, room::Maple.Room)
   direction = get(entity.data, "direction", "left")
   texture = get(entity.data, "texture", "objects/StrawberryJam2021/horizontalTempleGate/default")
   if direction == "center"
      sprite = Ahorn.getTextureSprite(texture*"/left00", "Gameplay")
      Ahorn.drawImage(ctx, texture*"/left00", 0, -3, 24, 0, sprite.width-24, 15)
      sprite = Ahorn.getTextureSprite(texture*"/right00", "Gameplay")
      Ahorn.drawImage(ctx, texture*"/right00", 72-sprite.width, -3, 0, 0, sprite.width-24, 15)
   else
      Ahorn.drawSprite(ctx, texture*"/"*direction*"00", 24, 5)
   end
end

end
