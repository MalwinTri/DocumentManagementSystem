# ---------- Build stage ----------
FROM node:20 AS build
WORKDIR /app

# 1) erst nur package-Dateien kopieren (Cache!)
COPY package*.json ./

# 2) devDependencies mit installieren (Tailwind/PostCSS/Vite)
RUN npm ci --include=dev

# 3) restlichen Code + Configs kopieren
COPY . .

# 4) Vite-Build (führt PostCSS+Tailwind aus)
RUN npm run build

# ---------- Runtime stage ----------
FROM nginx:alpine AS runtime

# optional: eigene nginx-config (verwende EINE der beiden Varianten)
# Variante A – default.conf:
# COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf
# Variante B – komplette nginx.conf:
# COPY nginx/nginx.conf /etc/nginx/nginx.conf

# gebaute Dateien deployen
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
