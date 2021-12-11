module SJ2021AutoSkipDialogCutsceneTrigger

using ..Ahorn, Maple

@mapdef Trigger "SJ2021/AutoSkipDialogCutsceneTrigger" AutoSkipDialogCutsceneTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    endLevel::Bool=false, onlyOnce::Bool=true, dialogId::String="", deathCount::Int=-1)

const placements = Ahorn.PlacementDict(
    "Auto-Skip Dialog Cutscene (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        AutoSkipDialogCutsceneTrigger,
        "rectangle"
    )
)

end