#!/usr/bin/env python3
"""
build_luna.py
=============
Packages a Unity WebGL build for submission to Unity Luna (formerly ironSource Luna).

Luna accepts two submission formats:
  1. A single inlined HTML file (≤ 5 MB)
  2. A ZIP containing index.html + all assets

This script produces BOTH:
  Builds/Luna/TapBlitz_Luna.html          ← inlined single file
  Builds/Luna/TapBlitz_Luna_package.zip   ← zip package (fallback)

Usage:
  python3 BuildConfig/build_luna.py <webgl_build_dir> <output_dir> [options]

Options:
  --title    <str>   Game title (default: TapBlitz)
  --ios      <url>   iOS App Store URL
  --android  <url>   Google Play URL

Example:
  python3 BuildConfig/build_luna.py Builds/WebGL Builds/Luna \\
    --title "TapBlitz" \\
    --ios "https://apps.apple.com/app/id123456" \\
    --android "https://play.google.com/store/apps/details?id=com.studio.tapblitz"

Requirements:
  Python 3.8+, no third-party packages
"""

import sys
import os
import base64
import zipfile
import shutil
import re
import json
import argparse
from pathlib import Path
from datetime import datetime

# ── Constants ─────────────────────────────────────────────────────────────────

WARN_MB  = 2.0
ERROR_MB = 5.0

# ── CLI ───────────────────────────────────────────────────────────────────────

def parse_args():
    p = argparse.ArgumentParser(description="Luna playable ad packager")
    p.add_argument("build_dir",  help="Unity WebGL build directory")
    p.add_argument("output_dir", help="Output directory for Luna package")
    p.add_argument("--title",    default="TapBlitz",  help="Game title")
    p.add_argument("--ios",      default="https://apps.apple.com/app/id000000000", help="iOS URL")
    p.add_argument("--android",  default="https://play.google.com/store/apps/details?id=com.studio.tapblitz", help="Android URL")
    return p.parse_args()

# ── Helpers ───────────────────────────────────────────────────────────────────

def log(msg): print(f"[build_luna] {msg}")

def read_bytes(path): 
    with open(path, "rb") as f: return f.read()

def read_text(path):
    with open(path, "r", encoding="utf-8") as f: return f.read()

def b64(data): return base64.b64encode(data).decode("utf-8")

def find(directory, pattern):
    matches = list(directory.rglob(pattern))
    return matches[0] if matches else None

def size_mb(path): return path.stat().st_size / (1024 * 1024)

def dir_size_mb(path):
    total = sum(f.stat().st_size for f in path.rglob("*") if f.is_file())
    return total / (1024 * 1024)

# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    args       = parse_args()
    build_dir  = Path(args.build_dir)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    log(f"Build dir : {build_dir}")
    log(f"Output dir: {output_dir}")
    log(f"Title     : {args.title}")

    build_sub = build_dir / "Build"
    if not build_sub.exists():
        log("ERROR: No 'Build' subfolder. Is this a Unity WebGL output?")
        sys.exit(1)

    # Locate build files
    loader_js    = find(build_sub, "*.loader.js")
    framework_js = find(build_sub, "*.framework.js.gz") or find(build_sub, "*.framework.js")
    data_file    = find(build_sub, "*.data.gz") or find(build_sub, "*.data")
    wasm_file    = find(build_sub, "*.wasm.gz") or find(build_sub, "*.wasm")

    if not all([loader_js, framework_js, data_file, wasm_file]):
        log("ERROR: Missing Unity WebGL build files.")
        sys.exit(1)

    is_gz = framework_js.suffix == ".gz"
    log(f"Gzip compressed: {is_gz}")
    log(f"Files found:")
    log(f"  loader   : {loader_js.name}")
    log(f"  framework: {framework_js.name}")
    log(f"  data     : {data_file.name}")
    log(f"  wasm     : {wasm_file.name}")

    # ── 1. Build single inlined HTML ──────────────────────────────────────────
    log("Building inlined HTML…")
    html = build_inlined_html(
        read_text(loader_js),
        b64(read_bytes(framework_js)),
        b64(read_bytes(data_file)),
        b64(read_bytes(wasm_file)),
        is_gz, args.title, args.ios, args.android
    )

    safe_title = re.sub(r'[^\w]', '_', args.title)
    html_path = output_dir / f"{safe_title}_Luna.html"
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(html)

    html_mb = size_mb(html_path)
    log(f"HTML output : {html_path.name}")
    log(f"HTML size   : {html_mb:.2f} MB")

    # ── 2. Build ZIP package ──────────────────────────────────────────────────
    log("Building ZIP package…")
    zip_path = output_dir / f"{safe_title}_Luna_package.zip"
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        # Walk the entire WebGL build
        for file in build_dir.rglob("*"):
            if file.is_file():
                arcname = file.relative_to(build_dir)
                zf.write(file, arcname)

    zip_mb = size_mb(zip_path)
    log(f"ZIP output  : {zip_path.name}")
    log(f"ZIP size    : {zip_mb:.2f} MB")

    # ── 3. Write luna_config.json ─────────────────────────────────────────────
    config = {
        "title":       args.title,
        "iosUrl":      args.ios,
        "androidUrl":  args.android,
        "builtAt":     datetime.now().isoformat(),
        "htmlSizeMb":  round(html_mb, 3),
        "zipSizeMb":   round(zip_mb, 3)
    }
    config_path = output_dir / "luna_build_info.json"
    with open(config_path, "w") as f:
        json.dump(config, f, indent=2)
    log(f"Build info  : {config_path.name}")

    # ── 4. Size checks ────────────────────────────────────────────────────────
    log("━━━━━━━━━━━━━━━━━━━━━━━━━")
    check_size("HTML", html_mb)
    log("━━━━━━━━━━━━━━━━━━━━━━━━━")
    log("Done! Submit to Luna dashboard:")
    log("  https://luna.unity.com")

def check_size(label, mb):
    if mb > ERROR_MB:
        log(f"[{label}] ⛔ {mb:.2f} MB — EXCEEDS 5 MB LIMIT. Luna will reject this!")
    elif mb > WARN_MB:
        log(f"[{label}] ⚠️ {mb:.2f} MB — Over 2 MB recommended. Check Luna limits.")
    else:
        log(f"[{label}] ✅ {mb:.2f} MB — Within Luna recommended limits.")

# ── HTML builder ──────────────────────────────────────────────────────────────

def build_inlined_html(loader_js, framework_b64, data_b64, wasm_b64,
                        is_gz, title, ios_url, android_url):
    decompress = """
    async function decodeB64(b64) {
      var bin = atob(b64), arr = new Uint8Array(bin.length);
      for (var i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);""" + ("""
      var ds = new DecompressionStream('gzip'), w = ds.writable.getWriter();
      w.write(arr); w.close();
      var chunks = [], r = ds.readable.getReader();
      while (true) { var {done,value} = await r.read(); if (done) break; chunks.push(value); }
      var total = chunks.reduce((s,a)=>s+a.length,0), out = new Uint8Array(total), off=0;
      chunks.forEach(c=>{out.set(c,off);off+=c.length;});
      return out.buffer;""" if is_gz else """
      return arr.buffer;""") + """
    }"""

    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1,user-scalable=no">
  <title>{title}</title>
  <!--
    {title} — Luna Playable Ad
    Built    : {ts}
    Platform : Unity Luna (unity.com/products/luna)
    iOS URL  : {ios_url}
    Android  : {android_url}
  -->
  <style>
    *{{margin:0;padding:0;box-sizing:border-box}}
    html,body{{width:100%;height:100%;overflow:hidden;background:#0d0d1a}}
    #c{{width:100%;height:100%;display:block;touch-action:none}}
    #ld{{position:fixed;inset:0;display:flex;flex-direction:column;align-items:center;
         justify-content:center;background:#0d0d1a;color:#fff;font-family:sans-serif;
         font-size:16px;gap:14px;transition:opacity .4s;z-index:999}}
    .bar{{width:180px;height:6px;background:rgba(255,255,255,.12);border-radius:3px;overflow:hidden}}
    .fill{{height:100%;background:linear-gradient(90deg,#a855f7,#3b82f6);
           border-radius:3px;transition:width .2s;width:0%}}
  </style>
</head>
<body>
<div id="ld">
  <div>{title}</div>
  <div class="bar"><div class="fill" id="pf"></div></div>
  <div style="font-size:12px;opacity:.5">Loading…</div>
</div>
<canvas id="c" tabindex="-1"></canvas>
<script>
// ── Luna runtime detection ────────────────────────────────────────────────────
(function(){{
  var detected = typeof Luna!=='undefined' ? 'Luna'
    : typeof gameReady==='function' ? 'Mintegral'
    : typeof mraid!=='undefined' ? 'MRAID'
    : 'Generic';
  console.log('[TapBlitz] Ad network detected:', detected);
  // Store URLs for Luna CTA handler
  window._iosUrl     = '{ios_url}';
  window._androidUrl = '{android_url}';
}})();

// ── Unity loader (inlined) ─────────────────────────────────────────────────
{loader_js}

// ── Asset data ────────────────────────────────────────────────────────────
var _fw = "{framework_b64}";
var _dt = "{data_b64}";
var _wm = "{wasm_b64}";

{decompress}

// ── Bootstrap ─────────────────────────────────────────────────────────────
(async function(){{
  var pf = document.getElementById('pf');
  var ld = document.getElementById('ld');
  try {{
    var [fw,dt,wm] = await Promise.all([decodeB64(_fw),decodeB64(_dt),decodeB64(_wm)]);
    pf.style.width = '45%';
    var mkUrl = (d,t) => URL.createObjectURL(new Blob([d],{{type:t}}));
    var config = {{
      dataUrl:            mkUrl(dt,'application/octet-stream'),
      frameworkUrl:       mkUrl(fw,'application/octet-stream'),
      codeUrl:            mkUrl(wm,'{("application/wasm" if not is_gz else "application/octet-stream")}'),
      streamingAssetsUrl: 'StreamingAssets',
      companyName:        'YourStudio',
      productName:        '{title}',
      productVersion:     '1.0'
    }};
    var inst = await createUnityInstance(document.getElementById('c'), config, p=>{{
      pf.style.width = (45+p*55)+'%';
    }});
    window.unityInstance = inst;
    // Inject store URL into Unity
    var url = /iP(hone|ad|od)/.test(navigator.userAgent) ? window._iosUrl : window._androidUrl;
    inst.SendMessage('LunaCtaHandler','SetStoreUrl', url);
    inst.SendMessage('WebGLBridge','OnStoreUrlReceived', url);
    // Hide loader
    ld.style.opacity='0';
    setTimeout(()=>ld.style.display='none',400);
    // Notify Luna runtime
    if(typeof Luna!=='undefined'&&Luna.Unity) console.log('[TapBlitz] Luna runtime active.');
    window.parent.postMessage({{source:'TapBlitz',event:'adReady'}},'*');
  }} catch(e) {{
    console.error('[TapBlitz] Load failed:',e);
    ld.innerHTML='<div style="color:#f87171">Failed to load. Refresh to retry.</div>';
  }}
}})();
</script>
</body>
</html>"""

# ── Entry ─────────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    main()
