FROM libretranslate/libretranslate:latest

# Baixa apenas os modelos de EN e PT para reduzir tamanho
ENV LT_LOAD_ONLY="pt,en"
ENV LT_THREADS="4"
ENV LT_PORT="5000"

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:5000/status || exit 1
