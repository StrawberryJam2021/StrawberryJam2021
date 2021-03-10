module SJ2021ShardedRefill

using ..Ahorn, Maple

@mapdef Entity "SJ2021/RefillShard" ShardedRefill(x::Integer, y::Integer, spawnRefill::Bool=false, twoDashes::Bool=false, resetOnGround::Bool=true, oneUse::Bool=false, collectAmount::Integer=0)

const placements = Ahorn.PlacementDict(
    "Sharded Refill (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ShardedRefill
    )
)

Ahorn.nodeLimits(entity::ShardedRefill) = 0, -1

function getSprites(entity::ShardedRefill)
    spawnRefill = get(entity.data, "spawnRefill", false)
    twoDashes = get(entity.data, "twoDashes", false)

    shardSprite = twoDashes ? "objects/StrawberryJam2021/refillShard/two00" : "objects/StrawberryJam2021/refillShard/one00"
    mainSprite = spawnRefill ? (twoDashes ? "objects/refillTwo/outline" : "objects/refill/outline") : shardSprite

    return (mainSprite, shardSprite)
end

function Ahorn.selection(entity::ShardedRefill)
    x, y = Ahorn.position(entity)

    sprite, shard = getSprites(entity)

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]
    
    for node in get(entity.data, "nodes", ())
        nx, ny = node

        push!(res, Ahorn.getSpriteRectangle(shard, nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ShardedRefill)
    x, y = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = node

        Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ShardedRefill, room::Maple.Room)
    x, y = Ahorn.position(entity)

    sprite, shard = getSprites(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = node

        Ahorn.drawSprite(ctx, shard, nx, ny)
    end

    Ahorn.drawSprite(ctx, sprite, x, y)
end

end