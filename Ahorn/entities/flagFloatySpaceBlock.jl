module SJ2021FlagFloatySpaceBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/FlagFloatySpaceBlock" FlagFloatySpaceBlock(x::Integer, y::Integer, 
    width::Integer=8, height::Integer=8, tiletype::String="3", moveTime::Number=1.0, disableSpawnOffset::Bool=false, flag::String="")

function flagFloatySpaceBlockFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = [(x, y - height)]

    Ahorn.tileEntityFinalizer(entity)
end
    
const placements = Ahorn.PlacementDict(
    "Flag Floaty Space Block (SJ2021)" => Ahorn.EntityPlacement(
        FlagFloatySpaceBlock,
        "rectangle",
        Dict{String, Any}(),
        flagFloatySpaceBlockFinalizer
    )
)

Ahorn.editingOptions(entity::FlagFloatySpaceBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.nodeLimits(entity::FlagFloatySpaceBlock) = 1, 1

Ahorn.minimumSize(entity::FlagFloatySpaceBlock) = 8, 8
Ahorn.resizable(entity::FlagFloatySpaceBlock) = true, true

function Ahorn.selection(entity::FlagFloatySpaceBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])
    stopY = (stopY <= y) ? stopY : y
    entity.data["nodes"] = [(stopX, stopY)]

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::FlagFloatySpaceBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::FlagFloatySpaceBlock, room::Maple.Room)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])
    stopY = (stopY <= startY) ? stopY : startY
    entity.data["nodes"] = [(stopX, stopY)]

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    Ahorn.drawTileEntity(ctx, room, entity)
    entity.data["x"] = stopX
    entity.data["y"] = stopY
    Ahorn.drawTileEntity(ctx, room, entity)
    entity.data["x"] = startX
    entity.data["y"] = startY
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

end