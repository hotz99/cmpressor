import { error } from '@sveltejs/kit';

export const actions = {
  default: async ({ request, fetch }) => {
    // Parse form data from the client
    const formData = await request.formData();
    const email = formData.get('email');
    const password = formData.get('password');

    try {
      // Make the request to the ASP.NET API
      const response = await fetch('http://localhost:3000/users/sign_up', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        console.log('errorData:', errorData);
        throw error(response.status, errorData.message || 'Sign-up failed');
      }

      const data = await response.json();

      return { success: true, message: 'Sign-up successful', user: data };
    } catch (err) {
      console.error('Error during sign-up request:', err);
      throw error(500, 'Failed to send sign-up request');
    }
  },
};

