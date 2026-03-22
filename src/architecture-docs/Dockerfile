FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY landing.html /usr/share/nginx/html/index.html
COPY index.html /usr/share/nginx/html/architecture.html
COPY upgrade-planner.html /usr/share/nginx/html/upgrade-planner.html
COPY domain-services.html /usr/share/nginx/html/domain-services.html
COPY patient-management.html /usr/share/nginx/html/patient-management.html
COPY clinical-encounters.html /usr/share/nginx/html/clinical-encounters.html
COPY scheduling.html /usr/share/nginx/html/scheduling.html
COPY pharmacy.html /usr/share/nginx/html/pharmacy.html
COPY laboratory.html /usr/share/nginx/html/laboratory.html
COPY provider-management.html /usr/share/nginx/html/provider-management.html
COPY api-versions.html /usr/share/nginx/html/api-versions.html
COPY rto-rpo.html /usr/share/nginx/html/rto-rpo.html
COPY tcp-to-https.html /usr/share/nginx/html/tcp-to-https.html
COPY strangler-pattern.html /usr/share/nginx/html/strangler-pattern.html
EXPOSE 8080
