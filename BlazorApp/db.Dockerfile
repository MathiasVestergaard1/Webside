FROM mysql:8.4
COPY Accounts/setup.sql /docker-entrypoint-initdb.d/01-setup.sql