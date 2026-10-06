# Stage 1: build the static site.
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
# Dev dependencies are build tools here (esbuild, three.js); only dist/ reaches the final image.
RUN npm ci --ignore-scripts
COPY src ./src
COPY data ./data
COPY scrum ./scrum
COPY projects/lib-c/tests/data ./projects/lib-c/tests/data
COPY projects/topologie/samples ./projects/topologie/samples
RUN npm run build

# Stage 2: serve it with nginx.
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK CMD wget -qO- http://localhost/fr/index.html >/dev/null || exit 1
