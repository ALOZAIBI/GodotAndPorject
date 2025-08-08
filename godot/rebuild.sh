scons dev_build=yes platform=linuxbsd module_mono_enabled=yes
./bin/godot.linuxbsd.editor.dev.x86_64.mono --headless --generate-mono-glue modules/mono/glue
./modules/mono/build_scripts/build_assemblies.py --godot-output-dir=./bin --push-nupkgs-local ~/MyLocalNugetSource
