CREATE TABLE subscription_plans (
    subscription_plan_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    max_file_size_megabytes INT NOT NULL,
    max_conversion_mins INT NOT NULL,
    max_concurrent_conversions INT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    subscription_plan_id INT NOT NULL REFERENCES subscription_plans(subscription_plan_id),
    remaining_conversion_mins INT NOT NULL,
    current_concurrent_conversions INT NOT NULL,
    reset_date TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
