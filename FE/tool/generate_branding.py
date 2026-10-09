#!/usr/bin/env python3
"""
CourtGo Android Branding Asset Generator
Generates bold, high-contrast, professional CourtGo branding assets.
"""

import os
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

COLOR_PRIMARY = (8, 127, 109, 255)         # #087F6D - CourtGo Emerald Teal
COLOR_PRIMARY_DARK = (5, 104, 90, 255)     # #05685A - Deep Emerald
COLOR_PRIMARY_DEEPER = (3, 76, 66, 255)    # #034C42
COLOR_MINT = (221, 248, 239, 255)          # #DDF8EF - CourtGo Mint Accent
COLOR_WHITE = (255, 255, 255, 255)         # #FFFFFF
COLOR_TEXT_PRIMARY = (16, 24, 40, 255)     # #101828
COLOR_TEXT_SECONDARY = (102, 112, 133, 255)# #667085

DENSITIES = {
    "mdpi": {
        "icon": 48,
        "round": 48,
        "fg": 108,
        "splash_badge": 120,
        "splash_logo": 180,
    },
    "hdpi": {
        "icon": 72,
        "round": 72,
        "fg": 162,
        "splash_badge": 180,
        "splash_logo": 270,
    },
    "xhdpi": {
        "icon": 96,
        "round": 96,
        "fg": 216,
        "splash_badge": 240,
        "splash_logo": 360,
    },
    "xxhdpi": {
        "icon": 144,
        "round": 144,
        "fg": 324,
        "splash_badge": 360,
        "splash_logo": 540,
    },
    "xxxhdpi": {
        "icon": 192,
        "round": 192,
        "fg": 432,
        "splash_badge": 480,
        "splash_logo": 720,
    },
}

def render_racket_and_ball_bold(canvas_size, emblem_scale=0.66, on_dark=True):
    """
    Renders a bold, highly recognizable CourtGo racket + sports ball emblem.
    Designed specifically to be instantly readable at small icon sizes.
    Uses 4x supersampling for ultra-crisp antialiasing.
    """
    scale_factor = 4
    w = canvas_size * scale_factor
    h = canvas_size * scale_factor
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx = w / 2.0
    cy = h / 2.0
    # Base scale unit
    s = (w * emblem_scale) / 100.0

    frame_col = COLOR_WHITE if on_dark else COLOR_PRIMARY
    accent_col = COLOR_MINT if on_dark else COLOR_PRIMARY_DARK
    strings_col = (221, 248, 239, 230) if on_dark else (8, 127, 109, 210)
    ball_col = COLOR_MINT if on_dark else COLOR_PRIMARY
    ball_seam_col = COLOR_PRIMARY if on_dark else COLOR_WHITE

    # Center position of racket head
    head_cx = cx + 8 * s
    head_cy = cy - 8 * s
    rot_angle = -42  # degrees tilt

    # Draw racket head on high-res layer for clean rotation
    head_dim = int(90 * s)
    head_img = Image.new("RGBA", (head_dim * 2, head_dim * 2), (0, 0, 0, 0))
    head_draw = ImageDraw.Draw(head_img)
    hcx = head_dim
    hcy = head_dim
    rx = 29 * s  # horizontal radius
    ry = 38 * s  # vertical radius

    # 1. Bold court strings (mesh grid) inside racket head
    grid_spacing = 8.5 * s
    line_w = max(2, int(3.6 * s))
    
    # Vertical strings
    for dx in [-1.5, -0.5, 0.5, 1.5]:
        x = hcx + dx * grid_spacing
        x_norm = (x - hcx) / rx
        if abs(x_norm) < 0.90:
            max_dy = ry * math.sqrt(1 - x_norm**2) * 0.88
            head_draw.line(
                [(x, hcy - max_dy), (x, hcy + max_dy)],
                fill=strings_col,
                width=line_w
            )

    # Horizontal strings
    for dy in [-2.0, -1.0, 0.0, 1.0, 2.0]:
        y = hcy + dy * grid_spacing
        y_norm = (y - hcy) / ry
        if abs(y_norm) < 0.90:
            max_dx = rx * math.sqrt(1 - y_norm**2) * 0.88
            head_draw.line(
                [(hcx - max_dx, y), (hcx + max_dx, y)],
                fill=strings_col,
                width=line_w
            )

    # 2. Bold racket head outer frame
    frame_w = max(3, int(8.2 * s))
    head_draw.ellipse(
        [hcx - rx, hcy - ry, hcx + rx, hcy + ry],
        outline=frame_col,
        width=frame_w
    )

    # Rotate head image
    rotated_head = head_img.rotate(rot_angle, resample=Image.Resampling.BICUBIC, center=(hcx, hcy))
    img.paste(rotated_head, (int(head_cx - hcx), int(head_cy - hcy)), rotated_head)

    # 3. Handle & Throat
    handle_len = 42 * s
    handle_dx = -math.sin(math.radians(48)) * handle_len
    handle_dy = math.cos(math.radians(48)) * handle_len
    handle_start_x = head_cx - math.sin(math.radians(42)) * (ry - 2*s)
    handle_start_y = head_cy + math.cos(math.radians(42)) * (ry - 2*s)
    handle_end_x = handle_start_x + handle_dx
    handle_end_y = handle_start_y + handle_dy

    # Sturdy V-throat
    throat_w = int(7.8 * s)
    draw.line(
        [(handle_start_x - 3.5*s, handle_start_y - 2*s), (handle_start_x + handle_dx*0.35, handle_start_y + handle_dy*0.35)],
        fill=frame_col,
        width=throat_w
    )
    draw.line(
        [(handle_start_x + 3.5*s, handle_start_y - 2*s), (handle_start_x + handle_dx*0.35, handle_start_y + handle_dy*0.35)],
        fill=frame_col,
        width=throat_w
    )

    # Main Grip
    grip_w = int(9.6 * s)
    grip_start_x = handle_start_x + handle_dx * 0.26
    grip_start_y = handle_start_y + handle_dy * 0.26
    draw.line(
        [(grip_start_x, grip_start_y), (handle_end_x, handle_end_y)],
        fill=frame_col,
        width=grip_w
    )

    # Butt cap
    cap_r = 5.6 * s
    draw.ellipse(
        [handle_end_x - cap_r, handle_end_y - cap_r, handle_end_x + cap_r, handle_end_y + cap_r],
        fill=accent_col
    )

    # Grip tape stripes (mint)
    for gt in [0.42, 0.58, 0.74, 0.90]:
        gx = grip_start_x + (handle_end_x - grip_start_x) * gt
        gy = grip_start_y + (handle_end_y - grip_start_y) * gt
        perp_dx = math.cos(math.radians(48)) * (5.2 * s)
        perp_dy = math.sin(math.radians(48)) * (5.2 * s)
        draw.line(
            [(gx - perp_dx, gy + perp_dy), (gx + perp_dx, gy - perp_dy)],
            fill=accent_col,
            width=max(2, int(2.4 * s))
        )

    # 4. Bold Sports Ball (Upper-right of racket)
    ball_cx = head_cx + 31 * s
    ball_cy = head_cy - 22 * s
    ball_r = 16.5 * s

    # Ball subtle glow on dark
    if on_dark:
        glow_r = ball_r + 3.2 * s
        draw.ellipse(
            [ball_cx - glow_r, ball_cy - glow_r, ball_cx + glow_r, ball_cy + glow_r],
            fill=(221, 248, 239, 70)
        )

    # Ball solid body (mint)
    draw.ellipse(
        [ball_cx - ball_r, ball_cy - ball_r, ball_cx + ball_r, ball_cy + ball_r],
        fill=ball_col
    )

    # Bold tennis seam
    seam_w = max(2, int(3.4 * s))
    draw.arc(
        [ball_cx - ball_r*0.9, ball_cy - ball_r*1.15, ball_cx + ball_r*1.15, ball_cy + ball_r*0.9],
        start=75,
        end=205,
        fill=ball_seam_col,
        width=seam_w
    )
    draw.arc(
        [ball_cx - ball_r*1.15, ball_cy - ball_r*0.9, ball_cx + ball_r*0.9, ball_cy + ball_r*1.15],
        start=255,
        end=25,
        fill=ball_seam_col,
        width=seam_w
    )

    return img.resize((canvas_size, canvas_size), Image.Resampling.LANCZOS)

def create_adaptive_foreground(size):
    # For Android adaptive icon 108dp base:
    # safe zone is center 72dp diameter (66.6% of canvas).
    # emblem_scale = 0.62 fits comfortably within safe zone and is large and readable!
    return render_racket_and_ball_bold(size, emblem_scale=0.62, on_dark=True)

def create_legacy_icon(size, round_shape=False):
    scale = 4
    w = size * scale
    h = size * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    margin = int(w * 0.05)
    box = [margin, margin, w - margin, h - margin]
    radius = (w - 2 * margin) // 2 if round_shape else int(w * 0.22)

    # Drop shadow
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    shadow_offset = int(w * 0.03)
    shadow_box = [margin, margin + shadow_offset, w - margin, h - margin + shadow_offset]
    sdraw.rounded_rectangle(shadow_box, radius=radius, fill=(0, 0, 0, 75))
    shadow = shadow.filter(ImageFilter.GaussianBlur(int(w * 0.032)))
    img.paste(shadow, (0, 0), shadow)

    # Base shape with gradient
    base_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    bdraw = ImageDraw.Draw(base_layer)
    bdraw.rounded_rectangle(box, radius=radius, fill=COLOR_PRIMARY)

    for y in range(margin, h - margin):
        ratio = (y - margin) / float(h - 2 * margin)
        r = int(COLOR_PRIMARY[0] * (1 - ratio * 0.28) + COLOR_PRIMARY_DARK[0] * (ratio * 0.28))
        g = int(COLOR_PRIMARY[1] * (1 - ratio * 0.28) + COLOR_PRIMARY_DARK[1] * (ratio * 0.28))
        b = int(COLOR_PRIMARY[2] * (1 - ratio * 0.28) + COLOR_PRIMARY_DARK[2] * (ratio * 0.28))
        bdraw.line([(margin, y), (w - margin, y)], fill=(r, g, b, 255))

    mask = Image.new("L", (w, h), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.rounded_rectangle(box, radius=radius, fill=255)
    base_layer.putalpha(mask)

    # Inner highlight border
    border_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    brdraw = ImageDraw.Draw(border_layer)
    brdraw.rounded_rectangle(box, radius=radius, outline=(255, 255, 255, 55), width=max(2, int(w * 0.018)))
    border_layer.putalpha(mask)

    img.paste(base_layer, (0, 0), base_layer)
    img.paste(border_layer, (0, 0), border_layer)

    # Foreground racket & ball
    fg = render_racket_and_ball_bold(w, emblem_scale=0.68, on_dark=True)
    img.paste(fg, (0, 0), fg)

    return img.resize((size, size), Image.Resampling.LANCZOS)

def create_splash_badge(size):
    scale = 3
    w = size * scale
    h = size * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Android 12+ masks splash icon to 66% diameter circle.
    # We use margin=17% so the circular badge sits comfortably inside the mask with no clipping.
    margin = int(w * 0.17)
    badge_size = w - 2 * margin
    badge_box = [margin, margin, w - margin, h - margin]

    # Soft ambient shadow
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    shadow_box = [margin, margin + int(w * 0.025), w - margin, h - margin + int(w * 0.025)]
    sdraw.ellipse(shadow_box, fill=(8, 127, 109, 60))
    shadow = shadow.filter(ImageFilter.GaussianBlur(int(w * 0.028)))
    img.paste(shadow, (0, 0), shadow)

    # Circular badge background
    draw.ellipse(badge_box, fill=COLOR_PRIMARY)

    # Subtle gradient
    grad_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(grad_layer)
    for y in range(margin, h - margin):
        ratio = (y - margin) / float(badge_size)
        r = int(COLOR_PRIMARY[0] * (1 - ratio * 0.22) + COLOR_PRIMARY_DARK[0] * (ratio * 0.22))
        g = int(COLOR_PRIMARY[1] * (1 - ratio * 0.22) + COLOR_PRIMARY_DARK[1] * (ratio * 0.22))
        b = int(COLOR_PRIMARY[2] * (1 - ratio * 0.22) + COLOR_PRIMARY_DARK[2] * (ratio * 0.22))
        gdraw.line([(margin, y), (w - margin, y)], fill=(r, g, b, 255))
    
    mask = Image.new("L", (w, h), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.ellipse(badge_box, fill=255)
    grad_layer.putalpha(mask)
    img.paste(grad_layer, (0, 0), grad_layer)

    # Inner highlight ring
    draw.ellipse(badge_box, outline=(255, 255, 255, 55), width=int(scale * 2.2))

    # Bold emblem centered inside circular badge
    emblem = render_racket_and_ball_bold(badge_size, emblem_scale=0.68, on_dark=True)
    img.paste(emblem, (margin, margin), emblem)

    return img.resize((size, size), Image.Resampling.LANCZOS)

def create_splash_logo_full(size):
    scale = 2
    w = size * scale
    h = size * scale
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    badge_size = int(w * 0.40)
    badge_radius = int(badge_size * 0.26)
    badge_cx = w // 2
    badge_cy = int(h * 0.38)

    badge_box = [
        badge_cx - badge_size // 2,
        badge_cy - badge_size // 2,
        badge_cx + badge_size // 2,
        badge_cy + badge_size // 2
    ]

    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    shadow_box = [badge_box[0], badge_box[1] + int(w * 0.025), badge_box[2], badge_box[3] + int(w * 0.025)]
    sdraw.rounded_rectangle(shadow_box, radius=badge_radius, fill=(8, 127, 109, 50))
    shadow = shadow.filter(ImageFilter.GaussianBlur(int(w * 0.028)))
    img.paste(shadow, (0, 0), shadow)

    draw.rounded_rectangle(badge_box, radius=badge_radius, fill=COLOR_PRIMARY)
    draw.rounded_rectangle(badge_box, radius=badge_radius, outline=(255, 255, 255, 50), width=int(scale * 2))

    emblem = render_racket_and_ball_bold(badge_size, emblem_scale=0.70, on_dark=True)
    img.paste(emblem, (badge_box[0], badge_box[1]), emblem)

    font_bold = None
    font_sub = None
    for font_path in [
        "C:\\Windows\\Fonts\\segoeuib.ttf",
        "C:\\Windows\\Fonts\\arialbd.ttf",
        "C:\\Windows\\Fonts\\segoeui.ttf",
    ]:
        if os.path.exists(font_path):
            try:
                font_bold = ImageFont.truetype(font_path, int(52 * scale))
                font_sub = ImageFont.truetype(font_path, int(18 * scale))
                break
            except Exception:
                pass

    if font_bold is None:
        font_bold = ImageFont.load_default()
        font_sub = ImageFont.load_default()

    title_text = "CourtGo"
    t_bbox = draw.textbbox((0, 0), title_text, font=font_bold)
    tw = t_bbox[2] - t_bbox[0]
    th = t_bbox[3] - t_bbox[1]
    title_x = (w - tw) // 2
    title_y = badge_box[3] + int(24 * scale)
    draw.text((title_x, title_y), title_text, fill=COLOR_PRIMARY, font=font_bold)

    sub_text = "Sport Court Booking"
    s_bbox = draw.textbbox((0, 0), sub_text, font=font_sub)
    sw = s_bbox[2] - s_bbox[0]
    sub_x = (w - sw) // 2
    sub_y = title_y + th + int(12 * scale)
    draw.text((sub_x, sub_y), sub_text, fill=COLOR_TEXT_SECONDARY, font=font_sub)

    return img.resize((size, size), Image.Resampling.LANCZOS)

def main():
    base_res = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "android", "app", "src", "main", "res"))
    assets_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "assets"))

    print(f"Generating bold assets into Android res: {base_res}")
    print(f"Generating bold assets into Flutter assets: {assets_dir}")

    for density, sizes in DENSITIES.items():
        mipmap_dir = os.path.join(base_res, f"mipmap-{density}")
        drawable_density_dir = os.path.join(base_res, f"drawable-{density}")
        os.makedirs(mipmap_dir, exist_ok=True)
        os.makedirs(drawable_density_dir, exist_ok=True)

        print(f"[{density}] Generating bold icons...")
        icon_img = create_legacy_icon(sizes["icon"], round_shape=False)
        icon_img.save(os.path.join(mipmap_dir, "ic_launcher.png"), "PNG")

        round_img = create_legacy_icon(sizes["round"], round_shape=True)
        round_img.save(os.path.join(mipmap_dir, "ic_launcher_round.png"), "PNG")

        fg_img = create_adaptive_foreground(sizes["fg"])
        fg_img.save(os.path.join(mipmap_dir, "ic_launcher_foreground.png"), "PNG")

        badge_img = create_splash_badge(sizes["splash_badge"])
        badge_img.save(os.path.join(drawable_density_dir, "splash_badge.png"), "PNG")

        logo_img = create_splash_logo_full(sizes["splash_logo"])
        logo_img.save(os.path.join(drawable_density_dir, "splash_logo.png"), "PNG")

    base_drawable = os.path.join(base_res, "drawable")
    os.makedirs(base_drawable, exist_ok=True)
    create_splash_badge(240).save(os.path.join(base_drawable, "splash_badge.png"), "PNG")
    create_splash_logo_full(360).save(os.path.join(base_drawable, "splash_logo.png"), "PNG")

    icons_asset_dir = os.path.join(assets_dir, "icons")
    common_asset_dir = os.path.join(assets_dir, "images", "common")
    os.makedirs(icons_asset_dir, exist_ok=True)
    os.makedirs(common_asset_dir, exist_ok=True)

    print("Generating high-resolution Flutter assets...")
    create_legacy_icon(512, round_shape=False).save(os.path.join(icons_asset_dir, "courtgo_icon.png"), "PNG")
    create_legacy_icon(512, round_shape=True).save(os.path.join(icons_asset_dir, "courtgo_icon_round.png"), "PNG")
    create_splash_badge(512).save(os.path.join(common_asset_dir, "courtgo_badge.png"), "PNG")
    create_splash_logo_full(720).save(os.path.join(common_asset_dir, "courtgo_logo.png"), "PNG")

    print("Bold branding assets regenerated successfully!")

if __name__ == "__main__":
    main()
