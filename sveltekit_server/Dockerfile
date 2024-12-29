FROM node:23.1-alpine

WORKDIR /app

# copy package files and install dependencies
COPY ./sveltekit_server/package*.json ./
RUN npm ci

# copy the rest of the application code
COPY ./sveltekit_server/ .

RUN npm run build

RUN npm prune --production

ENV NODE_ENV=production

EXPOSE 3000

CMD [ "node", "build" ]