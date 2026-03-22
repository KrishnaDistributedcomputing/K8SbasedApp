FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY landing.html /usr/share/nginx/html/index.html
COPY index.html /usr/share/nginx/html/architecture.html
COPY upgrade-planner.html /usr/share/nginx/html/upgrade-planner.html
EXPOSE 8080
