# Stage 1: the React dashboard (D43), built with its own dependencies; the site copies its dist/.
FROM node:22-alpine AS dashboard
WORKDIR /app/projects/react
COPY projects/react/package.json projects/react/package-lock.json ./
RUN npm ci --ignore-scripts
COPY projects/react/index.html projects/react/vite.config.ts projects/react/tsconfig.json projects/react/tsconfig.app.json ./
COPY projects/react/src ./src
# What the dashboard imports from the site: its colours (rule 6) and the WebAssembly topology reader.
COPY src/assets/tokens.css src/assets/topo-api.js src/assets/meshlib-api.js /app/src/assets/
RUN npm run build

# Stage 2: the Angular dashboard (D44), which shares the React one's types, computations, texts and styles.
FROM node:22-alpine AS angular
WORKDIR /app/projects/angular
COPY projects/angular/package.json projects/angular/package-lock.json ./
RUN npm ci --ignore-scripts
COPY projects/angular/angular.json projects/angular/tsconfig.json projects/angular/tsconfig.app.json ./
COPY projects/angular/src ./src
COPY projects/react/src /app/projects/react/src
COPY src/assets/tokens.css /app/src/assets/
RUN npm run build

# Stage 3: build the static site.
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
# Dev dependencies are build tools here (esbuild, three.js, sql.js); only dist/ reaches the final image.
# Every file the build reads must be copied below: tests/unit/docker-context.test.mjs builds from this list.
RUN npm ci --ignore-scripts
COPY src ./src
COPY data ./data
COPY scrum ./scrum
COPY projects/lib-c/tests/data ./projects/lib-c/tests/data
COPY projects/topologie/samples ./projects/topologie/samples
# SQL playground: schema, examples, campaign and the CSV reader shared with the benchmark script.
COPY projects/sql/sqlite ./projects/sql/sqlite
COPY projects/sql/data/measurements.csv ./projects/sql/data/
COPY projects/sql/bench/csv.mjs ./projects/sql/bench/
# Maille playground: the examples shown on the project page.
COPY projects/langage/examples ./projects/langage/examples
# LaTeX editor and generalized maps course: their sources are bundled by the build.
COPY projects/latex/src ./projects/latex/src
COPY projects/latex/article ./projects/latex/article
COPY projects/gcartes/src ./projects/gcartes/src
COPY projects/gcartes/course.json ./projects/gcartes/
# ML results page: what the DVC pipeline wrote.
COPY projects/ml/metrics.json projects/ml/confusion.json projects/ml/params.yaml ./projects/ml/
COPY projects/ml/export/pointnet.json ./projects/ml/export/
# War statistics computed by the Ada program.
COPY projects/bataille/data/stats.json ./projects/bataille/data/
# Tic-tac-toe move book computed by the Python program.
COPY projects/morpion/data/book.json ./projects/morpion/data/
COPY projects/parallele/data/bench.json ./projects/parallele/data/
COPY --from=dashboard /app/projects/react/dist ./projects/react/dist
COPY --from=angular /app/projects/angular/dist ./projects/angular/dist
RUN npm run build

# Stage 4: serve it with nginx.
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK CMD wget -qO- http://localhost/fr/index.html >/dev/null || exit 1
