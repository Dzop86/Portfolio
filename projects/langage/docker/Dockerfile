# Pinned tools for the web build of Maille (scripts/build-web.sh): Flex and Bison generate the C parser,
# js_of_ocaml compiles the interpreter. The same image runs locally and in CI, so the committed
# outputs can be rebuilt byte for byte.
FROM ocaml/opam:debian-12-ocaml-5.2
USER root
RUN apt-get update && apt-get install -y --no-install-recommends flex=2.6.4-8.2 bison=2:3.8.2+dfsg-1+b1 \
 && rm -rf /var/lib/apt/lists/*
USER opam
RUN opam install -y dune.3.24.2 js_of_ocaml.6.4.1 js_of_ocaml-compiler.6.4.1 && opam clean -a
