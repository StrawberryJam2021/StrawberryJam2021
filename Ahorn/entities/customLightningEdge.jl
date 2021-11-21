module SJ2021CustomLightningEdge
using ..Ahorn, Maple

@mapdef Entity "SJ2021/CustomLightningEdge" CustomLightningEdge(x::Integer, y::Integer, width::Integer=8, height::Integer=8, direction::String="Up", color1::String="fcf579", color2::String="8cf7e2")

const placements = Ahorn.PlacementDict()

const directions = ["Right", "Up", "Down", "Left"]

for i in directions
	key = "Custom Lightning Edge ($(i)) (Strawberry Jam 2021)"
	placements[key] = Ahorn.EntityPlacement(
		CustomLightningEdge,
		"rectangle",
		Dict{String, Any}(
			"direction" => "$(i)",
			"width" => (i == "Right" || i == "Left" ? 16 : 8),
			"height" => (i == "Up" || i == "Down" ? 16 : 8),
			"interval" => 0.05
		)
	)
end

function ColorFix(v::String, alpha::Float64=1.0)
	if length(strip(v)) == 0
		return (1.0, 1.0, 1.0, 1.0)
	end
    if v in keys(Ahorn.XNAColors.colors)
        w = get(Ahorn.XNAColors.colors, v, (1.0, 1.0, 1.0, 1.0))
        return (w[1], w[2], w[3], alpha)
    end
	w = ""
	if length(v) == 8
		v = SubString(v, 2)
		if(length(v) < 6) 
			return (1.0, 1.0, 1.0, 1.0)
		end
	end
    temp = Ahorn.argb32ToRGBATuple(parse(Int, v, base=16))[1:3] ./ 255
    color = (temp[1], temp[2], temp[3], alpha)
    return color
end

Ahorn.editingOptions(entity::CustomLightningEdge) = Dict{String, Any}(
	"color1" => sort(collect(keys(Ahorn.XNAColors.colors))),
	"color2" => sort(collect(keys(Ahorn.XNAColors.colors))),
	"direction" => String["Right", "Up", "Left", "Down"]
)

Ahorn.nodeLimits(entity::CustomLightningEdge) = 0, 1

function Ahorn.resizable(entity::CustomLightningEdge)
	dir = get(entity.data, "direction", "Up")
	if dir == "Right" || dir == "Left"
		return (false, true)
	elseif dir == "Up" || dir == "Down"
		return (true, false)
	else
		return (false, false)
	end
end

function Ahorn.selection(entity::CustomLightningEdge)
    x, y = Ahorn.position(entity)
	nodes = get(entity.data, "nodes", ())
	if isempty(nodes)
		dir = get(entity.data, "direction", "Up")
		if dir == "Right" || dir == "Left"
			return Ahorn.Rectangle(x - 2, y, 4, get(entity.data, "height", 8))
		else
			return Ahorn.Rectangle(x, y-2, get(entity.data, "width", 8), 4)
		end
	else
		nx, ny = Int.(nodes[1])
		return Ahorn.Rectangle[Ahorn.Rectangle(x-4, y-4, 8, 8), Ahorn.Rectangle(nx-4, ny-4, 8, 8)]
	end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomLightningEdge)
	x, y = Ahorn.position(entity)
	t = get(entity.data, "direction", "Up")
	colors = [get(entity.data, "color1", "fcf579"), get(entity.data, "color2", "8cf7e2")]
	nodes = get(entity.data, "nodes", ())
	if isempty(nodes)
		if t == "Up"
			len = entity.data["width"]
			nodes = [
				[(x, y-1), (x+len, y-1)],
				[(x, y+1), (x+len, y+1)]
			]
			Ahorn.drawArrow(ctx, x+len/2, y-1, x+len/2, y-8, headLength=4)
		elseif t == "Down"
			len = entity.data["width"]
			nodes = [
				[(x, y+1), (x+len, y+1)],
				[(x, y-1), (x+len, y-1)]
			]
			Ahorn.drawArrow(ctx, x+len/2, y+1, x+len/2, y+8, headLength=4)
		elseif t == "Right"
			len = entity.data["height"]
			nodes = [
				[(x+1, y), (x+1, y+len)],
				[(x-1, y), (x-1, y+len)]
			]
			Ahorn.drawArrow(ctx, x+1, y+len/2, x+8, y+len/2, headLength=4)
		else
			len = entity.data["height"]
			nodes = [
				[(x-1, y), (x-1, y+len)],
				[(x+1, y), (x+1, y+len)]
			]
			Ahorn.drawArrow(ctx, x-1, y+len/2, x-8, y+len/2, headLength=4)
		end
		Ahorn.drawLines(ctx, nodes[1], ColorFix(colors[1], 0.5))
		Ahorn.drawLines(ctx, nodes[2], ColorFix(colors[2], 0.5))
	else
		nx, ny = Int.(nodes[1])
		Ahorn.drawLines(ctx, [(x,y-1), (nx, ny-1)], ColorFix(colors[1], 0.5))
		Ahorn.drawLines(ctx, [(x,y+1), (nx, ny+1)], ColorFix(colors[2], 0.5))
	end
end
end