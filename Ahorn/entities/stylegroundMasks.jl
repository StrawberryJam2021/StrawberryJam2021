module SJ2021Masks

using ..Ahorn, Maple

@mapdef Entity "SJ2021/StylegroundMask" StylegroundMask(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    scrollX::Number=0.0, scrollY::Number=0.0, fade::String="None", customFade::String="circle", flag::String="", notFlag::Bool=false,
    tag::String="", alphaFrom::Number=0.0, alphaTo::Number=1.0, entityRenderer::Bool=false, behindFg::Bool=true)

@mapdef Entity "SJ2021/ColorGradeMask" ColorGradeMask(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    scrollX::Number=0.0, scrollY::Number=0.0, fade::String="None", customFade::String="circle", flag::String="", notFlag::Bool=false,
    colorGradeFrom::String="(current)", colorGradeTo::String="none", fadeFrom::Number=0.0, fadeTo::Number=1.0)

@mapdef Entity "SJ2021/BloomMask" BloomMask(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    scrollX::Number=0.0, scrollY::Number=0.0, fade::String="None", customFade::String="circle", flag::String="", notFlag::Bool=false,
    baseFrom::Number=-1.0, baseTo::Number=-1.0, strengthFrom::Number=-1.0, strengthTo::Number=-1.0)

@mapdef Entity "SJ2021/LightingMask" LightingMask(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    scrollX::Number=0.0, scrollY::Number=0.0, fade::String="None", customFade::String="circle", flag::String="", notFlag::Bool=false,
    lightingFrom::Number=-1.0, lightingTo::Number=0.0, addBase::Bool=true)

@mapdef Entity "SJ2021/AllInOneMask" AllInOneMask(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    scrollX::Number=0.0, scrollY::Number=0.0, fade::String="None", customFade::String="circle", flag::String="", notFlag::Bool=false,
    stylemaskTag::String="", styleAlphaFrom::Number=0.0, styleAlphaTo::Number=1.0, entityRenderer::Bool=false, styleBehindFg::Bool=true,
    colorGradeFrom::String="(current)", colorGradeTo::String="(current)", colorGradeFadeFrom::Number=0.0, colorGradeFadeTo::Number=1.0,
    bloomBaseFrom::Number=-1.0, bloomBaseTo::Number=-1.0, bloomStrengthFrom::Number=-1.0, bloomStrengthTo::Number=-1.0,
    lightingFrom::Number=-1.0, lightingTo::Number=-1.0, addBaseLight::Bool=true)

const placements = Ahorn.PlacementDict(
    "Styleground Mask (Strawberry Jam 2021)" => Ahorn.EntityPlacement(StylegroundMask, "rectangle"),
    "Color Grade Mask (Strawberry Jam 2021)" => Ahorn.EntityPlacement(ColorGradeMask, "rectangle"),
    "Lighting Mask (Strawberry Jam 2021)" => Ahorn.EntityPlacement(LightingMask, "rectangle"),
    "Bloom Mask (Strawberry Jam 2021)" => Ahorn.EntityPlacement(BloomMask, "rectangle"),
    "All In One Mask (Strawberry Jam 2021)" => Ahorn.EntityPlacement(AllInOneMask, "rectangle"),
)

const maskColors = Dict{Type, Tuple{Number, Number, Number}}(
    StylegroundMask => (0.4, 0.8, 0.8),
    ColorGradeMask => (0.8, 0.8, 0.4),
    LightingMask => (0.4, 0.4, 0.4),
    BloomMask => (1.0, 1.0, 1.0),
    AllInOneMask => (0.4, 0.4, 1.0)
)

const masksUnion = Union{StylegroundMask, ColorGradeMask, LightingMask, BloomMask, AllInOneMask}

const fadeOptions = ["None", "LeftToRight", "RightToLeft", "TopToBottom", "BottomToTop", "Custom"]
const colorGradeOptions = [["(current)", "(core)"] ; Maple.color_grades]

tiletypeOptions() = merge(Dict{String, String}(
    "(Current)" => "(current)",
    "(Air)" => "0"
), Ahorn.tiletypeEditingOptions())

const maskDict = Dict{String, Any}(
    "fade" => fadeOptions
)
const colorGradeDict = Dict{String, Any}(
    "colorGradeFrom" => colorGradeOptions,
    "colorGradeTo" => colorGradeOptions,
)
const tileDict = Dict{String, Any}(
    "fgTiletypeFrom" => tiletypeOptions(),
    "fgTiletypeTo" => tiletypeOptions(),
    "bgTiletypeFrom" => tiletypeOptions(),
    "bgTiletypeTo" => tiletypeOptions()
)


Ahorn.minimumSize(entity::masksUnion) = 8, 8
Ahorn.resizable(entity::masksUnion) = true, true

Ahorn.editingOptions(entity::StylegroundMask) = maskDict
Ahorn.editingOptions(entity::LightingMask) = maskDict
Ahorn.editingOptions(entity::BloomMask) = maskDict
Ahorn.editingOptions(entity::ColorGradeMask) = merge(maskDict, colorGradeDict)
Ahorn.editingOptions(entity::AllInOneMask) = merge(maskDict, colorGradeDict)

Ahorn.editingOrder(entity::StylegroundMask) = ["x", "y", "width", "height", "scrollX", "scrollY", "alphaFrom", "alphaTo", "tag", "flag", "fade", "customFade"]
Ahorn.editingOrder(entity::BloomMask) = ["x", "y", "width", "height", "scrollX", "scrollY", "baseFrom", "baseTo", "strengthFrom", "strengthTo", "flag", "customFade"]
Ahorn.editingOrder(entity::LightingMask) = ["x", "y", "width", "height", "scrollX", "scrollY", "lightingFrom", "lightingTo", "flag", "customFade"]
Ahorn.editingOrder(entity::ColorGradeMask) = ["x", "y", "width", "height", "scrollX", "scrollY", "colorGradeFrom", "colorGradeTo", "fadeFrom", "fadeTo", "flag", "customFade"]
Ahorn.editingOrder(entity::AllInOneMask) = ["x", "y", "width", "height", "scrollX", "scrollY", "bloomBaseFrom", "bloomBaseTo", "bloomStrengthFrom", "bloomStrengthTo", "lightingFrom", "lightingTo", "colorGradeFrom", "colorGradeTo", "colorGradeFadeFrom", "colorGradeFadeTo", "styleAlphaFrom", "styleAlphaTo", "stylemaskTag", "flag", "fade", "customFade"]

Ahorn.selection(entity::masksUnion) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::masksUnion, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    r, g, b = get(maskColors, typeof(entity), (1.0, 1.0, 1.0))
    Ahorn.drawRectangle(ctx, 0, 0, width, height, (r, g, b, 0.4), (r, g, b, 1.0))
end

end