import { error, redirect } from '@sveltejs/kit';

export const actions = {
  default: async ({ request, fetch, cookies }) => {
    const formData = await request.formData();
    const email = formData.get('email');
    const password = formData.get('password');

    try {
      const response = await fetch('http://localhost:3000/users/sign_in', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        console.log('errorData:', errorData);
        throw error(response.status, errorData.message || 'Sign-in failed');
      }

      const { token } = await response.json();

      return { success: true, token };
    } catch (err) {
      console.error("Error during sign-in request: ", err);
      throw error(500, 'Failed to send sign-in request');
    }
  },
};
