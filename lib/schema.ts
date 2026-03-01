import { pgTable, serial, varchar, boolean, timestamp, text, integer, uniqueIndex, index } from 'drizzle-orm/pg-core';

export const user = pgTable('user', {
  id: varchar('id', { length: 128 }).primaryKey(),
  name: varchar('name', { length: 255 }),
  email: varchar('email', { length: 255 }).notNull(),
  emailVerified: boolean('email_verified').default(false),
  image: varchar('image', { length: 500 }),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow(),
}, (table) => {
  return {
    emailIdx: uniqueIndex('user_email_idx').on(table.email),
  };
});

export const session = pgTable('session', {
  id: varchar('id', { length: 128 }).primaryKey(),
  expiresAt: timestamp('expires_at'),
  token: varchar('token', { length: 255 }).notNull(),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow(),
  ipAddress: varchar('ip_address', { length: 255 }),
  userAgent: varchar('user_agent', { length: 500 }),
  userId: varchar('user_id', { length: 128 }).notNull().references(() => user.id, { onDelete: 'cascade' }),
}, (table) => {
  return {
    tokenIdx: uniqueIndex('session_token_idx').on(table.token),
    userIdIdx: index('session_user_id_idx').on(table.userId),
  };
});

export const account = pgTable('account', {
  id: varchar('id', { length: 128 }).primaryKey(),
  accountId: varchar('account_id', { length: 255 }).notNull(),
  providerId: varchar('provider_id', { length: 255 }).notNull(),
  userId: varchar('user_id', { length: 128 }).notNull().references(() => user.id, { onDelete: 'cascade' }),
  accessToken: text('access_token'),
  refreshToken: text('refresh_token'),
  idToken: text('id_token'),
  accessTokenExpiresAt: timestamp('access_token_expires_at'),
  refreshTokenExpiresAt: timestamp('refresh_token_expires_at'),
  scope: text('scope'),
  password: text('password'),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow(),
}, (table) => {
  return {
    providerIdIdx: index('account_provider_id_idx').on(table.providerId),
    userIdIdx: index('account_user_id_idx').on(table.userId),
  };
});

export const verification = pgTable('verification', {
  id: varchar('id', { length: 128 }).primaryKey(),
  identifier: varchar('identifier', { length: 255 }).notNull(),
  value: text('value'),
  expiresAt: timestamp('expires_at').notNull(),
  createdAt: timestamp('created_at'),
  updatedAt: timestamp('updated_at'),
}, (table) => {
  return {
    identifierIdx: index('verification_identifier_idx').on(table.identifier),
  };
});
