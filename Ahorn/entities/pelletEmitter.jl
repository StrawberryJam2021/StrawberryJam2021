module SJ2021PelletEmitter

using ..Ahorn, Maple

const DEFAULT_PELLET_SPEED = 100.0
const DEFAULT_PELLET_DELAY = 0.25
const DEFAULT_WIGGLE_AMOUNT = 2.0
const DEFAULT_WIGGLE_FREQUENCY = 2.0

const width = 16.0
const length = 16.0

@mapdef Entity "SJ2021/PelletEmitterUp" PelletEmitterUp(
    x::Integer, y::Integer,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletCount::Integer=1,
    pelletDelay::Real=DEFAULT_PELLET_DELAY,
    cassetteIndex::Int=0, collideWithSolids::Bool=true,
    wiggleAmount::Real=DEFAULT_WIGGLE_AMOUNT, wiggleFrequency::Real=DEFAULT_WIGGLE_FREQUENCY
)

@mapdef Entity "SJ2021/PelletEmitterDown" PelletEmitterDown(
    x::Integer, y::Integer,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletCount::Integer=1,
    pelletDelay::Real=DEFAULT_PELLET_DELAY,
    cassetteIndex::Int=0, collideWithSolids::Bool=true,
    wiggleAmount::Real=DEFAULT_WIGGLE_AMOUNT, wiggleFrequency::Real=DEFAULT_WIGGLE_FREQUENCY
)

@mapdef Entity "SJ2021/PelletEmitterLeft" PelletEmitterLeft(
    x::Integer, y::Integer,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletCount::Integer=1,
    pelletDelay::Real=DEFAULT_PELLET_DELAY,
    cassetteIndex::Int=0, collideWithSolids::Bool=true,
    wiggleAmount::Real=DEFAULT_WIGGLE_AMOUNT, wiggleFrequency::Real=DEFAULT_WIGGLE_FREQUENCY
)

@mapdef Entity "SJ2021/PelletEmitterRight" PelletEmitterRight(
    x::Integer, y::Integer,
    pelletSpeed::Real=DEFAULT_PELLET_SPEED, pelletCount::Integer=1,
    pelletDelay::Real=DEFAULT_PELLET_DELAY,
    cassetteIndex::Int=0, collideWithSolids::Bool=true,
    wiggleAmount::Real=DEFAULT_WIGGLE_AMOUNT, wiggleFrequency::Real=DEFAULT_WIGGLE_FREQUENCY
)

const placements = Ahorn.PlacementDict(
    "Pellet Emitter (Up) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterUp,
    ),
    "Pellet Emitter (Down) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterDown,
    ),
    "Pellet Emitter (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterLeft,
    ),
    "Pellet Emitter (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PelletEmitterRight,
    )
)

const colorNames = Dict{String, Int}(
    "Both" => -1,
    "Blue" => 0,
    "Rose" => 1,
)

const pelletEmitterUnion = Union{PelletEmitterUp, PelletEmitterDown, PelletEmitterLeft, PelletEmitterRight}

function Ahorn.selection(entity::PelletEmitterUp)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - width / 2, y - length, width, length)
end

function Ahorn.selection(entity::PelletEmitterDown)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - width / 2, y, width, length)
end

function Ahorn.selection(entity::PelletEmitterLeft)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - length, y - width / 2, length, width)
end

function Ahorn.selection(entity::PelletEmitterRight)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y - width / 2, length, width)
end

Ahorn.editingOptions(entity::pelletEmitterUnion) = Dict{String, Any}(
    "cassetteIndex" => colorNames
)

sprite_path = "objects/StrawberryJam2021/pelletEmitter"

function spriteForEntity(entity::pelletEmitterUnion)
    index = get(entity.data, "cassetteIndex", 0)
    prefix = index == 0 ? "blue" : "pink"
    return "$(sprite_path)/$(prefix)/emitter00"
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterUp, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForEntity(entity), 4, 0, jx=0.5, jy=0, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterDown, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForEntity(entity), 4, 0, jx=0.5, jy=0, sy=-1, rot=-pi/2)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterLeft, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForEntity(entity), 0, 0, jx=0, jy=0.5, sx=-1, rot=0)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PelletEmitterRight, room::Maple.Room) = Ahorn.drawSprite(ctx, spriteForEntity(entity), 0, 0, jx=0, jy=0.5, rot=0)

end
