{
  description = "my project description";

  outputs = { self, nixpkgs }: {
    devShell.x86_64-linux =
      with nixpkgs.outputs.legacyPackages.x86_64-linux; mkShellNoCC {
        nativeBuildInputs = [
          love
        ];
        LD_LIBRARY_PATH="${lib.makeLibraryPath [love]}:/tmp";
      };
  };
}
