module SJ2021MomentumBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/VariantToggleController" VariantToggleController(x::Integer, y::Integer, flag::String="", variantList::String="")

const placements = Ahorn.PlacementDict(
   "Variant Toggle Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
      VariantToggleController,
      "rectangle"
   )
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::VariantToggleController, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultBlackColor, Ahorn.defaultWhiteColor)
end


function Ahorn.selection(entity::VariantToggleController)
    x, y = Ahorn.position(entity)
    return [Ahorn.Rectangle(x, y, 8, 8)]
end

end