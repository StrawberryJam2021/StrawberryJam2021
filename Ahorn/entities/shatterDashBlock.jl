module SJ2021ShatterDashBlock
using ..Ahorn, Maple

@mapdef Entity "SJ2021/ShatterDashBlock" ShatterDashBlock(x::Integer, y::Integer, width::Integer=8, height::Integer=8, tiletype::String="3", blendin::Bool=true, permanent::Bool=false, FreezeTime::Number=0.1, SpeedRequirement::Number=480.0, ShakeTime::Number=0.3, SpeedDecrease::Number=80.0)

const placements = Ahorn.PlacementDict(
    "Shatter Dash Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ShatterDashBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer
    )
)

Ahorn.editingOptions(entity::ShatterDashBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::ShatterDashBlock) = 8, 8
Ahorn.resizable(entity::ShatterDashBlock) = true, true

Ahorn.selection(entity::ShatterDashBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ShatterDashBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end
