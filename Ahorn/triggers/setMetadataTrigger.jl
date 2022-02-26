module SJ2021SetMetadataTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/SetMetadataTrigger" SetMetadataTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, theoInBooster::Bool=false)

const placements = Ahorn.PlacementDict(
    "Set Metadata Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SetMetadataTrigger,
        "rectangle"
    )
)

end